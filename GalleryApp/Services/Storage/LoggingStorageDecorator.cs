using GalleryApp.Services.Logging;

namespace GalleryApp.Services.Storage;

public class LoggingStorageDecorator : IStorageService
{
    private readonly IStorageService _inner;
    private readonly IActionLogger _logger;

    public LoggingStorageDecorator(IStorageService inner, IActionLogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await _logger.LogAsync(
            action: "Storage.Save",
            entityType: "StorageObject",
            entityId: key,
            details: $"contentType={contentType}",
            ct: ct
        );

        await _inner.SaveAsync(key, content, contentType, ct);
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        await _logger.LogAsync(
            action: "Storage.OpenRead",
            entityType: "StorageObject",
            entityId: key,
            details: null,
            ct: ct
        );

        return await _inner.OpenReadAsync(key, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _logger.LogAsync(
            action: "Storage.Delete",
            entityType: "StorageObject",
            entityId: key,
            details: null,
            ct: ct
        );

        await _inner.DeleteAsync(key, ct);
    }
}
