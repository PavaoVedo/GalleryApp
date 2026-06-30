using GalleryApp.Models.ViewModels;
using GalleryApp.Services.Functional;
using GalleryApp.Services.Plans;
using Xunit;

namespace GalleryApp.Tests.Unit;

public class PhotoFunctionsTests
{
    [Fact]
    public void NormalizeTags_Trims_Lowercases_StripsHash_AndDeduplicates()
    {
        var result = PhotoFunctions.NormalizeTags(" #Sky, sky , #Sea ,SEA");
        Assert.Equal(new[] { "sky", "sea" }, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeTags_NullOrWhitespace_ReturnsEmpty(string? raw)
    {
        Assert.Empty(PhotoFunctions.NormalizeTags(raw));
    }

    [Fact]
    public void NormalizeTags_CapsAtTwenty()
    {
        var raw = string.Join(",", Enumerable.Range(0, 50).Select(i => $"tag{i}"));
        Assert.Equal(20, PhotoFunctions.NormalizeTags(raw).Count);
    }

    [Fact]
    public void ValidateUpload_OversizeFile_Fails()
    {
        var policy = new FreePlanPolicy();                 
        var result = PhotoFunctions.ValidateUpload(policy, 3 * 1024 * 1024, uploadsToday: 0);
        Assert.False(result.IsSuccess);
        Assert.Contains("too large", result.Error);
    }

    [Fact]
    public void ValidateUpload_DailyLimitReached_Fails()
    {
        var policy = new FreePlanPolicy();                 
        var result = PhotoFunctions.ValidateUpload(policy, 1024, uploadsToday: 3);
        Assert.False(result.IsSuccess);
        Assert.Contains("Daily upload limit", result.Error);
    }

    [Fact]
    public void ValidateUpload_WithinLimits_Succeeds()
    {
        var policy = new GoldPlanPolicy();
        var result = PhotoFunctions.ValidateUpload(policy, 1024, uploadsToday: 1);
        Assert.True(result.IsSuccess);
        Assert.Equal(1024, result.Value);
    }

    [Fact]
    public void BuildPhotoFilters_BuildsOneFunctionPerActiveCriterion()
    {
        var model = new PhotoSearchViewModel
        {
            AuthorEmail = "a@b.com",
            MinSizeMb = 1,
            Hashtags = "x,y"
        };

        var filters = PhotoFunctions.BuildPhotoFilters(model);
        Assert.Equal(4, filters.Count);   
    }
}