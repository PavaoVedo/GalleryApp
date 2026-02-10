using GalleryApp.Data;
using GalleryApp.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GalleryApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var logs = _db.ActionLogs.AsNoTracking();

        var grouped = await logs
            .Where(l => l.UserId != null)
            .GroupBy(l => new { l.UserId, l.UserEmail })
            .Select(g => new AdminUserStatsRow
            {
                UserId = g.Key.UserId!,
                Email = g.Key.UserEmail,

                Uploads = g.Count(x => x.Action == "UploadPhoto"),
                DownloadsOriginal = g.Count(x => x.Action == "DownloadOriginal"),
                DownloadsProcessed = g.Count(x => x.Action == "DownloadProcessed"),
                Edits = g.Count(x => x.Action == "EditPhotoMetadata"),
                Searches = g.Count(x => x.Action == "SearchPhotos"),
                Deletes = g.Count(x => x.Action == "DeletePhoto" || x.Action == "AdminDeletePhoto"),

                LastActionUtc = g.Max(x => (DateTime?)x.TimestampUtc)
            })
            .OrderByDescending(x => x.LastActionUtc)
            .ToListAsync(ct);

        var idsMissingEmail = grouped.Where(x => string.IsNullOrWhiteSpace(x.Email)).Select(x => x.UserId).ToList();
        if (idsMissingEmail.Count > 0)
        {
            var emails = await _db.Users
                .Where(u => idsMissingEmail.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(ct);

            var map = emails.ToDictionary(x => x.Id, x => x.Email);

            foreach (var row in grouped)
                if (string.IsNullOrWhiteSpace(row.Email) && map.TryGetValue(row.UserId, out var email))
                    row.Email = email;
        }

        return View(grouped);
    }

    [HttpGet]
    public async Task<IActionResult> User(string id, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound();

        var lastAction = await _db.ActionLogs.AsNoTracking()
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.TimestampUtc)
            .Select(l => (DateTime?)l.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        var totalPhotos = await _db.Photos.AsNoTracking().CountAsync(p => p.UserId == id, ct);

        var recentLogs = await _db.ActionLogs.AsNoTracking()
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.TimestampUtc)
            .Take(100)
            .ToListAsync(ct);

        var recentPhotos = await _db.Photos.AsNoTracking()
            .Where(p => p.UserId == id)
            .OrderByDescending(p => p.UploadedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        var vm = new AdminUserDetailsViewModel
        {
            UserId = id,
            Email = user.Email,
            TotalPhotos = totalPhotos,
            LastActionUtc = lastAction,
            RecentLogs = recentLogs,
            RecentPhotos = recentPhotos
        };

        return View(vm);
    }
}
