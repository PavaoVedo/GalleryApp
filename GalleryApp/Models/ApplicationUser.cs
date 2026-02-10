using Microsoft.AspNetCore.Identity;

namespace GalleryApp.Models
{
    public enum Plan
    {
        Free = 0,
        Pro = 1,
        Gold = 2
    }

    public class ApplicationUser : IdentityUser
    {
        public Plan CurrentPlan { get; set; } = Plan.Free;

        public Plan? PendingPlan { get; set; }
        public DateOnly? PendingEffectiveDate { get; set; }

        public int UploadsTodayCount { get; set; } = 0;
        public DateOnly? UploadsTodayDate { get; set; }
    }
}
