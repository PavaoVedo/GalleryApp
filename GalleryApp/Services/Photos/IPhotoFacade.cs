namespace GalleryApp.Services.Photos;

public interface IPhotoFacade
{
    Task<Stream> OpenOriginalAsync(Guid photoId, CancellationToken ct = default);
    Task DeletePhotoAsync(Guid photoId, CancellationToken ct = default);
}