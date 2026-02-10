namespace GalleryApp.Services.Storage
{
    public interface IStorageService
    {
        Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default);
        Task<Stream> OpenReadAsync(string key, CancellationToken ct = default);
        Task DeleteAsync(string key, CancellationToken ct = default);
    }
}
