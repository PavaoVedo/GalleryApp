using System.Security.Claims;
using GalleryApp.Data;
using GalleryApp.Models;
using GalleryApp.Models.ViewModels;
using GalleryApp.Services.Images;
using GalleryApp.Services.Logging.Commands;
using GalleryApp.Services.Photos;               
using GalleryApp.Services.Plans;
using GalleryApp.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GalleryApp.Controllers;

public class PhotosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ActionCommandDispatcher _dispatcher;
    private readonly PhotoFacade _photoFacade;  

    public PhotosController(
        ApplicationDbContext db,
        IStorageService storage,
        IImageProcessor imageProcessor,
        ActionCommandDispatcher dispatcher,
        PhotoFacade photoFacade)                
    {
        _db = db;
        _storage = storage;
        _imageProcessor = imageProcessor;
        _dispatcher = dispatcher;
        _photoFacade = photoFacade;            
    }

    [Authorize]
    [HttpGet]
    public IActionResult Upload() => View();

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? description, string? hashtags, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please choose a file.");
            return View();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (user.UploadsTodayDate == null || user.UploadsTodayDate.Value != today)
        {
            user.UploadsTodayDate = today;
            user.UploadsTodayCount = 0;
        }

        var policy = PlanPolicyFactory.FromPlan(user.CurrentPlan);

        if (file.Length > policy.MaxBytesPerPhoto)
        {
            ModelState.AddModelError("", $"File too large for {policy.Name}. Max {policy.MaxBytesPerPhoto / (1024 * 1024)} MB.");
            return View();
        }

        if (user.UploadsTodayCount >= policy.MaxUploadsPerDay)
        {
            ModelState.AddModelError("", $"Daily upload limit reached for {policy.Name} ({policy.MaxUploadsPerDay}/day).");
            return View();
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var photoId = Guid.NewGuid();
        var key = $"photos/{photoId}{ext.ToLower()}";

        await using (var stream = file.OpenReadStream())
        {
            await _storage.SaveAsync(key, stream, file.ContentType ?? "application/octet-stream", ct);
        }

        var photo = new Photo
        {
            Id = photoId,
            UserId = userId,
            FileKey = key,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedAtUtc = DateTime.UtcNow,
            Description = description
        };

        var parsedTags = ParseTags(hashtags);

        foreach (var tag in parsedTags)
        {
            var existing = await _db.Hashtags.FirstOrDefaultAsync(h => h.Tag == tag, ct);
            if (existing == null)
            {
                existing = new Hashtag { Tag = tag };
                _db.Hashtags.Add(existing);
            }

            photo.PhotoHashtags.Add(new PhotoHashtag
            {
                Photo = photo,
                Hashtag = existing
            });
        }

        _db.Photos.Add(photo);
        user.UploadsTodayCount += 1;

        await _db.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(
            new LogActionCommand(
                action: "UploadPhoto",
                entityType: "Photo",
                entityId: photo.Id.ToString(),
                details: $"sizeBytes={photo.SizeBytes}; key={photo.FileKey}"
            ),
            ct
        );

        return RedirectToAction(nameof(Details), new { id = photo.Id });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos
            .Include(p => p.User)
            .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (photo == null) return NotFound();
        return View(photo);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> File(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo == null) return NotFound();

        var stream = await _photoFacade.OpenOriginalAsync(id, ct);
        return File(stream, photo.ContentType ?? "application/octet-stream");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DownloadOriginal(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo == null) return NotFound();

        var stream = await _storage.OpenReadAsync(photo.FileKey, ct);

        var downloadName = photo.OriginalFileName ?? Path.GetFileName(photo.FileKey);

        await _dispatcher.DispatchAsync(
            new LogActionCommand(
                action: "DownloadOriginal",
                entityType: "Photo",
                entityId: photo.Id.ToString(),
                details: photo.FileKey
            ),
            ct
        );

        return File(stream, photo.ContentType ?? "application/octet-stream", downloadName);
    }

    private static List<string> ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim().TrimStart('#'))
            .Where(t => t.Length > 0)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(20)
            .ToList();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos
            .Include(p => p.User)
            .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (photo == null) return NotFound();

        if (!CanEditPhoto(photo))
            return Forbid();

        var vm = new EditPhotoViewModel
        {
            Id = photo.Id,
            Description = photo.Description,
            Hashtags = string.Join(", ", photo.PhotoHashtags.Select(x => "#" + x.Hashtag.Tag)),
            PreviewUrl = Url.Action("File", "Photos", new { id = photo.Id }),
            AuthorEmail = photo.User?.Email,
            UploadedAtUtc = photo.UploadedAtUtc
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPhotoViewModel model, CancellationToken ct)
    {
        var photo = await _db.Photos
            .Include(p => p.User)
            .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
            .FirstOrDefaultAsync(p => p.Id == model.Id, ct);

        if (photo == null) return NotFound();

        if (!CanEditPhoto(photo))
            return Forbid();

        if (!ModelState.IsValid)
        {
            model.PreviewUrl = Url.Action("File", "Photos", new { id = photo.Id });
            model.AuthorEmail = photo.User?.Email;
            model.UploadedAtUtc = photo.UploadedAtUtc;
            return View(model);
        }

        photo.Description = model.Description?.Trim();

        var newTags = ParseTags(model.Hashtags);

        var currentTags = photo.PhotoHashtags
            .Select(ph => ph.Hashtag.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var wantedTags = newTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = photo.PhotoHashtags
            .Where(ph => !wantedTags.Contains(ph.Hashtag.Tag))
            .ToList();

        foreach (var ph in toRemove)
            photo.PhotoHashtags.Remove(ph);

        foreach (var tag in wantedTags)
        {
            if (currentTags.Contains(tag)) continue;

            var existing = await _db.Hashtags.FirstOrDefaultAsync(h => h.Tag == tag, ct);
            if (existing == null)
            {
                existing = new Hashtag { Tag = tag };
                _db.Hashtags.Add(existing);
            }

            photo.PhotoHashtags.Add(new PhotoHashtag
            {
                PhotoId = photo.Id,
                Hashtag = existing
            });
        }

        await _db.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(
            new LogActionCommand(
                action: "EditPhotoMetadata",
                entityType: "Photo",
                entityId: photo.Id.ToString(),
                details: $"descChanged={(photo.Description ?? "")}; tags={model.Hashtags}"
            ),
            ct
        );

        return RedirectToAction(nameof(Details), new { id = photo.Id });
    }

    private bool CanEditPhoto(Photo photo)
    {
        if (User.IsInRole("Admin")) return true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId != null && photo.UserId == userId;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo == null) return NotFound();

        if (!User.IsInRole("Admin") && photo.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Forbid();

        await _photoFacade.DeletePhotoAsync(id, ct);

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> AdminIndex(CancellationToken ct)
    {
        var photos = await _db.Photos
            .Include(p => p.User)
            .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
            .OrderByDescending(p => p.UploadedAtUtc)
            .Take(200)
            .ToListAsync(ct);

        return View(photos);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDelete(Guid id, CancellationToken ct)
    {
        await _photoFacade.DeletePhotoAsync(id, ct);
        return RedirectToAction(nameof(AdminIndex));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Search()
    {
        return View(new PhotoSearchViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(PhotoSearchViewModel model, CancellationToken ct)
    {
        var query = _db.Photos
            .Include(p => p.User)
            .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(model.AuthorEmail))
        {
            var email = model.AuthorEmail.Trim().ToLower();
            query = query.Where(p => p.User != null && p.User.Email != null && p.User.Email.ToLower().Contains(email));
        }

        if (model.MinSizeMb.HasValue)
        {
            var minBytes = (long)(model.MinSizeMb.Value * 1024 * 1024);
            query = query.Where(p => p.SizeBytes >= minBytes);
        }

        if (model.MaxSizeMb.HasValue)
        {
            var maxBytes = (long)(model.MaxSizeMb.Value * 1024 * 1024);
            query = query.Where(p => p.SizeBytes <= maxBytes);
        }

        if (model.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(model.FromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.UploadedAtUtc >= fromUtc);
        }

        if (model.ToDate.HasValue)
        {
            var toUtcExclusive = DateTime.SpecifyKind(model.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(p => p.UploadedAtUtc < toUtcExclusive);
        }

        var tags = ParseTags(model.Hashtags);
        foreach (var tag in tags)
        {
            var t = tag;
            query = query.Where(p => p.PhotoHashtags.Any(ph => ph.Hashtag.Tag == t));
        }

        model.Results = await query
            .OrderByDescending(p => p.UploadedAtUtc)
            .Take(200)
            .ToListAsync(ct);

        await _dispatcher.DispatchAsync(
            new LogActionCommand(
                action: "SearchPhotos",
                entityType: "Photo",
                entityId: null,
                details: $"tags={model.Hashtags}; author={model.AuthorEmail}; minMb={model.MinSizeMb}; maxMb={model.MaxSizeMb}; from={model.FromDate}; to={model.ToDate}; results={model.Results.Count}"
            ),
            ct
        );

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> AdminLogs(CancellationToken ct)
    {
        var logs = await _db.ActionLogs
            .OrderByDescending(l => l.TimestampUtc)
            .Take(300)
            .ToListAsync(ct);

        return View(logs);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> DownloadProcessed(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo == null) return NotFound();

        var vm = new DownloadProcessedViewModel
        {
            PhotoId = id,
            Format = "jpg"
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadProcessed(DownloadProcessedViewModel model, CancellationToken ct)
    {
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == model.PhotoId, ct);
        if (photo == null) return NotFound();

        await using var input = await _storage.OpenReadAsync(photo.FileKey, ct);
        var (bytes, contentType, extension) = await _imageProcessor.ProcessAsync(input, model, ct);

        await _dispatcher.DispatchAsync(
            new LogActionCommand(
                action: "DownloadProcessed",
                entityType: "Photo",
                entityId: photo.Id.ToString(),
                details: $"format={model.Format}; w={model.ResizeWidth}; h={model.ResizeHeight}; sepia={model.Sepia}; blur={model.Blur}"
            ),
            ct
        );

        var baseName = Path.GetFileNameWithoutExtension(photo.OriginalFileName ?? "photo");
        var downloadName = $"{baseName}_processed{extension}";

        return File(bytes, contentType, downloadName);
    }
}
