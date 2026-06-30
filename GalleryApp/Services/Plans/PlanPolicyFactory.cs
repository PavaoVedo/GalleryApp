using GalleryApp.Models;
using GalleryApp.Services.Functional;

namespace GalleryApp.Services.Plans
{
    public static class PlanPolicyFactory
    {
       
        private static readonly Func<Plan, IPlanPolicy> Resolver =
            ((Func<Plan, IPlanPolicy>)Create).Memoize();

        public static IPlanPolicy FromPlan(Plan plan) => Resolver(plan);

        private static IPlanPolicy Create(Plan plan) => plan switch
        {
            Plan.Free => new FreePlanPolicy(),
            Plan.Pro => new ProPlanPolicy(),
            Plan.Gold => new GoldPlanPolicy(),
            _ => new FreePlanPolicy()
        };
    }
}