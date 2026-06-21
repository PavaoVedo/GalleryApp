using GalleryApp.Models;
using GalleryApp.Models.ViewModels;
using GalleryApp.Services.Plans;
using SixLabors.ImageSharp.Processing;

namespace GalleryApp.Services.Functional;

public static class PhotoFunctions
{
    public static IReadOnlyList<string> NormalizeTags(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(t => t.Trim().TrimStart('#'))
                 .Where(t => t.Length > 0)
                 .Select(t => t.ToLowerInvariant())
                 .Distinct()
                 .Take(20)
                 .ToArray();

    public static Result<long> ValidateUpload(IPlanPolicy policy, long fileLength, int uploadsToday)
    {
        if (fileLength <= 0)
            return Result<long>.Fail("Please choose a file.");

        if (fileLength > policy.MaxBytesPerPhoto)
            return Result<long>.Fail(
                $"File too large for {policy.Name}. Max {policy.MaxBytesPerPhoto / (1024 * 1024)} MB.");

        if (uploadsToday >= policy.MaxUploadsPerDay)
            return Result<long>.Fail(
                $"Daily upload limit reached for {policy.Name} ({policy.MaxUploadsPerDay}/day).");

        return Result<long>.Ok(fileLength);
    }

    public static IReadOnlyList<Func<IQueryable<Photo>, IQueryable<Photo>>> BuildPhotoFilters(
        PhotoSearchViewModel m)
    {
        var filters = new List<Func<IQueryable<Photo>, IQueryable<Photo>>>();

        if (!string.IsNullOrWhiteSpace(m.AuthorEmail))
        {
            var email = m.AuthorEmail.Trim().ToLower();
            filters.Add(q => q.Where(p =>
                p.User != null && p.User.Email != null && p.User.Email.ToLower().Contains(email)));
        }

        if (m.MinSizeMb.HasValue)
        {
            var minBytes = (long)(m.MinSizeMb.Value * 1024 * 1024);
            filters.Add(q => q.Where(p => p.SizeBytes >= minBytes));
        }

        if (m.MaxSizeMb.HasValue)
        {
            var maxBytes = (long)(m.MaxSizeMb.Value * 1024 * 1024);
            filters.Add(q => q.Where(p => p.SizeBytes <= maxBytes));
        }

        if (m.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(m.FromDate.Value.Date, DateTimeKind.Utc);
            filters.Add(q => q.Where(p => p.UploadedAtUtc >= fromUtc));
        }

        if (m.ToDate.HasValue)
        {
            var toUtcExclusive = DateTime.SpecifyKind(m.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            filters.Add(q => q.Where(p => p.UploadedAtUtc < toUtcExclusive));
        }

        foreach (var tag in NormalizeTags(m.Hashtags))
        {
            var t = tag; 
            filters.Add(q => q.Where(p => p.PhotoHashtags.Any(ph => ph.Hashtag.Tag == t)));
        }

        return filters;
    }

    
    public static Func<IImageProcessingContext, IImageProcessingContext> BuildImagePipeline(
        DownloadProcessedViewModel o)
    {
        var steps = new List<Func<IImageProcessingContext, IImageProcessingContext>>();

        if (o.ResizeWidth.HasValue && o.ResizeHeight.HasValue)
            steps.Add(ctx => ctx.Resize(o.ResizeWidth.Value, o.ResizeHeight.Value));

        if (o.Sepia)
            steps.Add(ctx => ctx.Sepia());

        if (o.Blur > 0)
            steps.Add(ctx => ctx.GaussianBlur(o.Blur));

        return steps.ComposeAll(); 
}