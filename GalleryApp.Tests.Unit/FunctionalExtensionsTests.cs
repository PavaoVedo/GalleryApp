using GalleryApp.Services.Functional;
using Xunit;

namespace GalleryApp.Tests.Unit;

public class FunctionalExtensionsTests
{
    [Fact]
    public void Pipe_AppliesFunctionToValue()
    {
        Assert.Equal(6, 3.Pipe(x => x * 2));
    }

    [Fact]
    public void Compose_RunsFirstThenSecond()
    {
        Func<int, int> addOne = x => x + 1;
        Func<int, int> times2 = x => x * 2;
        var composed = addOne.Compose(times2);     
        Assert.Equal(8, composed(3));              
    }

    [Fact]
    public void ComposeAll_AppliesFunctionsLeftToRight()
    {
        var fns = new Func<int, int>[] { x => x + 1, x => x * 3 };
        var composed = fns.ComposeAll();
        Assert.Equal(12, composed(3));            
    }

    [Fact]
    public void Memoize_InvokesUnderlyingFunctionOncePerKey()
    {
        var calls = 0;
        Func<int, int> f = x => { calls++; return x * 2; };
        var memo = f.Memoize();

        memo(2); memo(2); memo(3);

        Assert.Equal(4, memo(2));
        Assert.Equal(2, calls);                  
    }
}