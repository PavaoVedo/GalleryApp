namespace GalleryApp.Services.Plans
{
    public class FreePlanPolicy : IPlanPolicy
    {
        public string Name => "FREE";
        public int MaxUploadsPerDay => 3;
        public long MaxBytesPerPhoto => 2 * 1024 * 1024; 
    }
}
