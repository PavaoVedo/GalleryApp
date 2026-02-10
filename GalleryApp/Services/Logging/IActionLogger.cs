namespace GalleryApp.Services.Logging;

public interface IActionLogger
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, CancellationToken ct = default);
}
