using System.Diagnostics;
using Castle.DynamicProxy;

namespace GalleryApp.Services.Aspects;

public sealed class TimingAspect : AsyncInterceptorBase
{
    private readonly ILogger<TimingAspect> _logger;

    public TimingAspect(ILogger<TimingAspect> logger) => _logger = logger;

    private static string Name(IInvocation i) =>
        $"{i.TargetType?.Name ?? i.Method.DeclaringType?.Name}.{i.Method.Name}";

    protected override async Task InterceptAsync(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        var sw = Stopwatch.StartNew();
        try { await proceed(invocation, proceedInfo); }
        finally
        {
            sw.Stop();
            _logger.LogInformation("[AOP:Timing] {Method} took {Ms} ms", Name(invocation), sw.ElapsedMilliseconds);
        }
    }

    protected override async Task<TResult> InterceptAsync<TResult>(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        var sw = Stopwatch.StartNew();
        try { return await proceed(invocation, proceedInfo); }
        finally
        {
            sw.Stop();
            _logger.LogInformation("[AOP:Timing] {Method} took {Ms} ms", Name(invocation), sw.ElapsedMilliseconds);
        }
    }
}