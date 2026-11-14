namespace InventoryApp.Domain;

/// <summary>Explicit success-or-failure value. No exceptions cross the domain boundary.</summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly AppError? _error;

    private Result(T? value, AppError? error)
    {
        _value = value;
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result is a failure: " + _error!.Message);

    public AppError Error => _error
        ?? throw new InvalidOperationException("Result is a success.");

    internal static Result<T> Success(T value) => new(value, null);
    internal static Result<T> Failure(AppError error) => new(default, error);
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);
    public static Result<T> Fail<T>(AppError error) => Result<T>.Failure(error);
    public static Result<T> Fail<T>(ErrorCode code, string message) => Result<T>.Failure(new AppError(code, message));
}