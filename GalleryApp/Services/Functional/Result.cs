namespace GalleryApp.Services.Functional;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool ok, T? value, string? error)
    {
        IsSuccess = ok;
        Value = value;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> f) =>
        IsSuccess ? Result<TOut>.Ok(f(Value!)) : Result<TOut>.Fail(Error!);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> f) =>
        IsSuccess ? f(Value!) : Result<TOut>.Fail(Error!);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}