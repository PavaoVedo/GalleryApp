using GalleryApp.Data;
using GalleryApp.Services.Logging.Commands;
using GalleryApp.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace GalleryApp.Services.Photos;

public class PhotoFacade
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly ActionCommandDispatcher _dispatcher;

    public PhotoFacade(ApplicationDbContext db, IStorageService storage, ActionCommandDispatcher dispatcher)
    {
        _db = db;
        _storage = storage;
        _dispatcher = dispatcher;
    }

    public async Task<Stream> OpenOriginalAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await _db.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photoId, ct)
            ?? throw new InvalidOperationException("Photo not found.");

        await _dispatcher.DispatchAsync(
            new LogActionCommand("Photo.OpenOriginal", "Photo", photoId.ToString()),
            ct
        );

        return await _storage.OpenReadAsync(photo.FileKey, ct);
    }

    public async Task DeletePhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await _db.Photos
            .Include(p => p.PhotoHashtags)
            .FirstOrDefaultAsync(p => p.Id == photoId, ct)
            ?? throw new InvalidOperationException("Photo not found.");

        await _storage.DeleteAsync(photo.FileKey, ct);

        if (photo.PhotoHashtags.Count > 0)
            _db.PhotoHashtags.RemoveRange(photo.PhotoHashtags);

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(
            new LogActionCommand("Photo.Delete", "Photo", photoId.ToString()),
            ct
        );
    }
}
