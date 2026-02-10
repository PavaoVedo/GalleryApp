using Microsoft.Extensions.Options;

namespace GalleryApp.Services.Storage;

public class LocalStorageOptions
{
    public string RootPath { get; set; } = "Storage";
}

public class LocalStorageService : IStorageService
{
    private readonly string _root;

    public LocalStorageService(IOptions<LocalStorageOptions> options)
    {
        _root = options.Value.RootPath;
        Directory.CreateDirectory(_root);
    }

    private string MapPath(string key)
    {
        key = key.Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_root, key);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = MapPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = MapPath(key);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = MapPath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
