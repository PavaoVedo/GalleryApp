using GalleryApp.Models.ViewModels;

namespace GalleryApp.Services.Images;

public interface IImageProcessor
{
    Task<(byte[] bytes, string contentType, string extension)> ProcessAsync(Stream input, DownloadProcessedViewModel options, CancellationToken ct);
}
