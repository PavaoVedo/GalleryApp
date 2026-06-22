using System.Diagnostics.Metrics;

namespace GalleryApp.Services.Metrics;

public sealed class GalleryMetrics
{
    public const string MeterName = "GalleryApp";

    private readonly Counter<long> _photosUploaded;
    private readonly Counter<long> _photosDeleted;
    private readonly Counter<long> _searchesPerformed;
    private readonly Counter<long> _bytesUploaded;
    private readonly Histogram<double> _imageProcessingMs;

    private int _uploadsInFlight;

    public GalleryMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _photosUploaded = meter.CreateCounter<long>(
            "galleryapp.photos.uploaded", unit: "{photo}",
            description: "Total number of photos uploaded.");

        _photosDeleted = meter.CreateCounter<long>(
            "galleryapp.photos.deleted", unit: "{photo}",
            description: "Total number of photos deleted.");

        _searchesPerformed = meter.CreateCounter<long>(
            "galleryapp.searches.performed", unit: "{search}",
            description: "Total number of searches executed.");

        _bytesUploaded = meter.CreateCounter<long>(
            "galleryapp.storage.bytes_uploaded", unit: "By",
            description: "Total bytes of original images uploaded.");

        _imageProcessingMs = meter.CreateHistogram<double>(
            "galleryapp.image.processing.duration", unit: "ms",
            description: "Time taken to process an image download request.");

        meter.CreateObservableGauge(
            "galleryapp.uploads.in_flight",
            () => _uploadsInFlight, unit: "{upload}",
            description: "Number of uploads currently being processed.");
    }

    public void PhotoUploaded(string plan, long sizeBytes)
    {
        var tag = new KeyValuePair<string, object?>("plan", plan);
        _photosUploaded.Add(1, tag);
        _bytesUploaded.Add(sizeBytes, tag);
    }

    public void PhotoDeleted() => _photosDeleted.Add(1);

    public void SearchPerformed(int resultCount) =>
        _searchesPerformed.Add(1, new KeyValuePair<string, object?>("has_results", resultCount > 0));

    public void RecordImageProcessing(double milliseconds, string format) =>
        _imageProcessingMs.Record(milliseconds, new KeyValuePair<string, object?>("format", format));

    public IDisposable TrackUpload()
    {
        Interlocked.Increment(ref _uploadsInFlight);
        return new UploadScope(this);
    }

    private void EndUpload() => Interlocked.Decrement(ref _uploadsInFlight);

    private sealed class UploadScope : IDisposable
    {
        private readonly GalleryMetrics _owner;
        private bool _disposed;
        public UploadScope(GalleryMetrics owner) => _owner = owner;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.EndUpload();
        }
    }
}