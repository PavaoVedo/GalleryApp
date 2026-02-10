using Microsoft.Extensions.Configuration;

namespace GalleryApp.Services.Storage;

public class StorageSelectorService : IStorageService
{
    private readonly IConfiguration _config;
    private readonly LocalStorageService _local;
    private readonly MinioStorageService _minio;

    public StorageSelectorService(
        IConfiguration config,
        LocalStorageService local,
        MinioStorageService minio)
    {
        _config = config;
        _local = local;
        _minio = minio;
    }

    private IStorageService Active
    {
        get
        {
            var provider = _config["Storage:Provider"]?.Trim();
            return string.Equals(provider, "Minio", StringComparison.OrdinalIgnoreCase)
                ? _minio
                : _local;
        }
    }

    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
        => Active.SaveAsync(key, content, contentType, ct);

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
        => Active.OpenReadAsync(key, ct);

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => Active.DeleteAsync(key, ct);
}
