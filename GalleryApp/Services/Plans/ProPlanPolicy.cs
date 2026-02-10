namespace GalleryApp.Services.Plans
{
    public class ProPlanPolicy : IPlanPolicy
    {
        public string Name => "PRO";
        public int MaxUploadsPerDay => 20;
        public long MaxBytesPerPhoto => 10 * 1024 * 1024; 
    }
}
