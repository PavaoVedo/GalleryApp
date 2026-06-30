using Castle.DynamicProxy;

namespace GalleryApp.Services.Aspects;
public sealed class LoggingAspect : AsyncInterceptorBase
{
    private readonly ILogger<LoggingAspect> _logger;

    public LoggingAspect(ILogger<LoggingAspect> logger) => _logger = logger;

    private static string Name(IInvocation i) =>
        $"{i.TargetType?.Name ?? i.Method.DeclaringType?.Name}.{i.Method.Name}";

    protected override async Task InterceptAsync(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        var name = Name(invocation);
        _logger.LogInformation("[AOP:Logging] -> {Method}", name);
        try
        {
            await proceed(invocation, proceedInfo);
            _logger.LogInformation("[AOP:Logging] <- {Method} completed", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AOP:Logging] !! {Method} threw", name);
            throw;
        }
    }

    protected override async Task<TResult> InterceptAsync<TResult>(
        IInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        var name = Name(invocation);
        _logger.LogInformation("[AOP:Logging] -> {Method}", name);
        try
        {
            var result = await proceed(invocation, proceedInfo);
            _logger.LogInformation("[AOP:Logging] <- {Method} completed", name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AOP:Logging] !! {Method} threw", name);
            throw;
        }
    }
}