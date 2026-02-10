using GalleryApp.Models;

namespace GalleryApp.Services.Plans
{
    public static class PlanPolicyFactory
    {
        public static IPlanPolicy FromPlan(Plan plan) => plan switch
        {
            Plan.Free => new FreePlanPolicy(),
            Plan.Pro => new ProPlanPolicy(),
            Plan.Gold => new GoldPlanPolicy(),
            _ => new FreePlanPolicy()
        };
    }
}
