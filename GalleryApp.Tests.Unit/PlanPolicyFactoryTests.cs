using GalleryApp.Models;
using GalleryApp.Services.Plans;
using Xunit;

namespace GalleryApp.Tests.Unit;

public class PlanPolicyFactoryTests
{
    [Theory]
    [InlineData(Plan.Free, "FREE", 3)]
    [InlineData(Plan.Pro, "PRO", 20)]
    [InlineData(Plan.Gold, "GOLD", 100)]
    public void FromPlan_ReturnsMatchingPolicy(Plan plan, string name, int maxUploads)
    {
        var policy = PlanPolicyFactory.FromPlan(plan);
        Assert.Equal(name, policy.Name);
        Assert.Equal(maxUploads, policy.MaxUploadsPerDay);
    }

    [Fact]
    public void FromPlan_IsMemoized_ReturnsSameInstance()
    {
        var a = PlanPolicyFactory.FromPlan(Plan.Pro);
        var b = PlanPolicyFactory.FromPlan(Plan.Pro);
        Assert.Same(a, b);
    }
}