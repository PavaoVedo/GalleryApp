namespace GalleryApp.Services.Plans
{
    public interface IPlanPolicy
    {
        int MaxUploadsPerDay { get; }
        long MaxBytesPerPhoto { get; } 
        string Name { get; }
    }
}
