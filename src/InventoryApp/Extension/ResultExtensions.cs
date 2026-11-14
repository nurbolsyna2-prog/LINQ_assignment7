using InventoryApp.Domain;

namespace InventoryApp.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// EXTENSION 2 - receiver Result&lt;T&gt;, returns Result&lt;U&gt;.
    /// Runs next only when this result succeeded; a failure is passed on unchanged
    /// and next is NOT called.
    /// </summary>
    public static Result<U> Then<T, U>(this Result<T> result, Func<T, Result<U>> next) =>
        result.IsSuccess ? next(result.Value) : Result.Fail<U>(result.Error);

    /// <summary>
    /// EXTENSION 3 (bonus) - receiver IEnumerable&lt;Result&lt;T&gt;&gt;.
    /// Enumerates once, stops at the first failure (which also disposes any open source) and returns
    /// either all values as a snapshot list or that first error.
    /// </summary>
    public static Result<IReadOnlyList<T>> Sequence<T>(this IEnumerable<Result<T>> results)
    {
        var list = new List<T>();
        foreach (var r in results)
        {
            if (!r.IsSuccess) return Result.Fail<IReadOnlyList<T>>(r.Error);
            list.Add(r.Value);
        }
        return Result.Ok<IReadOnlyList<T>>(list);
    }
}