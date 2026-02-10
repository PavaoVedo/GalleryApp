namespace GalleryApp.Services.Plans
{
    public class GoldPlanPolicy : IPlanPolicy
    {
        public string Name => "GOLD";
        public int MaxUploadsPerDay => 100;
        public long MaxBytesPerPhoto => 25 * 1024 * 1024; 
    }
}
