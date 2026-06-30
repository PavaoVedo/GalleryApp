using GalleryApp.Models.ViewModels;
using GalleryApp.Services.Functional;
using GalleryApp.Services.Metrics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace GalleryApp.Services.Images;

public class ImageSharpProcessor : IImageProcessor
{
    private readonly GalleryMetrics _metrics;

    public ImageSharpProcessor(GalleryMetrics metrics) => _metrics = metrics;

    public async Task<(byte[] bytes, string contentType, string extension)> ProcessAsync(
        Stream input, DownloadProcessedViewModel options, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var image = await Image.LoadAsync(input, ct);

            var pipeline = PhotoFunctions.BuildImagePipeline(options);
            image.Mutate(ctx => pipeline(ctx));

            options.Format = options.Format?.Trim().ToLowerInvariant() ?? "jpg";

            using var ms = new MemoryStream();

            return options.Format switch
            {
                "png" => await SaveAsync(image, ms, new PngEncoder(), "image/png", ".png", ct),
                "bmp" => await SaveAsync(image, ms, new BmpEncoder(), "image/bmp", ".bmp", ct),
                _ => await SaveAsync(image, ms, new JpegEncoder { Quality = 90 }, "image/jpeg", ".jpg", ct),
            };
        }
        finally
        {
            sw.Stop();
            _metrics.RecordImageProcessing(sw.Elapsed.TotalMilliseconds, options.Format ?? "jpg");
        }
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