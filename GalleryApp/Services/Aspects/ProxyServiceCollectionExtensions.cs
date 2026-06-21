using Castle.DynamicProxy;

namespace GalleryApp.Services.Aspects;


public static class ProxyServiceCollectionExtensions
{
    private static readonly ProxyGenerator Generator = new();

    public static IServiceCollection AddAspects(this IServiceCollection services)
    {
        services.AddScoped<LoggingAspect>();
        services.AddScoped<TimingAspect>();
        return services;
    }

    public static IServiceCollection AddProxiedScoped<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TImplementation>();
        services.AddScoped(provider =>
        {
            var target = provider.GetRequiredService<TImplementation>();
            var aspects = new IAsyncInterceptor[]
            {
                provider.GetRequiredService<LoggingAspect>(),
                provider.GetRequiredService<TimingAspect>()
            };
            return Generator.CreateInterfaceProxyWithTarget<TInterface>(target, aspects);
        });
        return services;
    }
}