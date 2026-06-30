using System.Collections.Concurrent;

namespace GalleryApp.Services.Functional;

public static class FunctionalExtensions
{
    public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> f) => f(input);

    public static Func<A, C> Compose<A, B, C>(this Func<A, B> f, Func<B, C> g) => x => g(f(x));

    public static Func<T, T> ComposeAll<T>(this IEnumerable<Func<T, T>> fns) =>
        fns.Aggregate<Func<T, T>, Func<T, T>>(x => x, (acc, f) => x => f(acc(x)));

    public static Func<TKey, TResult> Memoize<TKey, TResult>(this Func<TKey, TResult> f)
        where TKey : notnull
    {
        var cache = new ConcurrentDictionary<TKey, TResult>();
        return key => cache.GetOrAdd(key, f);
    }
}