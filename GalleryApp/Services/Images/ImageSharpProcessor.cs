using GalleryApp.Models.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace GalleryApp.Services.Images;

public class ImageSharpProcessor : IImageProcessor
{
    public async Task<(byte[] bytes, string contentType, string extension)> ProcessAsync(Stream input, DownloadProcessedViewModel options, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);

        if (options.ResizeWidth.HasValue && options.ResizeHeight.HasValue)
        {
            image.Mutate(x => x.Resize(options.ResizeWidth.Value, options.ResizeHeight.Value));
        }

        if (options.Sepia)
        {
            image.Mutate(x => x.Sepia());
        }

        if (options.Blur > 0)
        {
            image.Mutate(x => x.GaussianBlur(options.Blur));
        }

        options.Format = options.Format?.Trim().ToLowerInvariant() ?? "jpg";

        using var ms = new MemoryStream();

        return options.Format switch
        {
            "png" => await SaveAsync(image, ms, new PngEncoder(), "image/png", ".png", ct),
            "bmp" => await SaveAsync(image, ms, new BmpEncoder(), "image/bmp", ".bmp", ct),
            _ => await SaveAsync(image, ms, new JpegEncoder { Quality = 90 }, "image/jpeg", ".jpg", ct),
        };
    }

    private static async Task<(byte[] bytes, string contentType, string extension)> SaveAsync(
        Image image,
        MemoryStream ms,
        IImageEncoder encoder,
        string contentType,
        string extension,
        CancellationToken ct)
    {
        ms.Position = 0;
        ms.SetLength(0);

        await image.SaveAsync(ms, encoder, ct);
        return (ms.ToArray(), contentType, extension);
    }
}
