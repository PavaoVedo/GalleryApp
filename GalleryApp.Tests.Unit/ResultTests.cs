using GalleryApp.Services.Functional;
using Xunit;

namespace GalleryApp.Tests.Unit;

public class ResultTests
{
    [Fact]
    public void Map_TransformsValue_OnSuccess()
    {
        var result = Result<int>.Ok(2).Map(x => x * 10);
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void Map_PropagatesError_OnFailure()
    {
        var result = Result<int>.Fail("boom").Map(x => x * 10);
        Assert.False(result.IsSuccess);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void Bind_ChainsResults_OnSuccess()
    {
        var result = Result<int>.Ok(4).Bind(x => Result<string>.Ok($"v={x}"));
        Assert.True(result.IsSuccess);
        Assert.Equal("v=4", result.Value);
    }

    [Fact]
    public void Match_SelectsCorrectBranch()
    {
        Assert.Equal("ok:5", Result<int>.Ok(5).Match(v => $"ok:{v}", e => $"err:{e}"));
        Assert.Equal("err:x", Result<int>.Fail("x").Match(v => $"ok:{v}", e => $"err:{e}"));
    }
}