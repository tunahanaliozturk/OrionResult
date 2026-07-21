namespace Moongazing.OrionResult;

using System.Collections.Generic;
using System.Linq;

using Moongazing.Orion.Abstractions.Results;

/// <summary>
/// The outcome of an operation that can fail expectedly: either a success carrying a
/// <typeparamref name="T"/>, or a failure carrying one or more <see cref="Error"/>s. A
/// <c>readonly struct</c>, so the success path allocates nothing.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <remarks>
/// Construct via the implicit conversions (<c>return value;</c> / <c>return Error.NotFound(...);</c>)
/// or the <see cref="Result"/> factories. It implements the family's
/// <see cref="IOrionResult{TValue}"/>, so cross-cutting code reads any Orion result the same way.
/// <para>
/// <c>default(Result&lt;T&gt;)</c> is a success carrying <c>default(T)</c> - always construct one
/// deliberately rather than relying on the default.
/// </para>
/// </remarks>
public readonly struct Result<T> : IOrionResult<T>, IEquatable<Result<T>>
{
    private readonly T value;
    private readonly Error[]? errors;

    internal Result(T value)
    {
        this.value = value;
        errors = null;
    }

    internal Result(Error[] errors)
    {
        value = default!;
        this.errors = errors;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => errors is null;

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => errors is not null;

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> if read on a failure -
    /// guard with <see cref="IsSuccess"/> or use <see cref="Match{TOut}"/>/<see cref="OrElse(T)"/>.
    /// </summary>
    public T Value => IsSuccess
        ? value
        : throw new InvalidOperationException(
            $"Cannot read the value of a failed result. Errors: {string.Join("; ", Errors)}");

    /// <summary>The failures, or an empty list on success.</summary>
    public IReadOnlyList<Error> Errors => errors ?? [];

    /// <inheritdoc />
    OrionError? IOrionResult.Error => errors is { Length: > 0 } ? errors[0].ToOrionError() : null;

    /// <summary>Transform the success value, propagating a failure unchanged.</summary>
    /// <typeparam name="TOut">The mapped value type.</typeparam>
    /// <param name="map">The success-value transform.</param>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? new Result<TOut>(map(value)) : new Result<TOut>(errors!);
    }

    /// <summary>
    /// Chain another result-producing step onto a success, propagating a failure unchanged
    /// (railway composition).
    /// </summary>
    /// <typeparam name="TOut">The next step's value type.</typeparam>
    /// <param name="bind">The next step.</param>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? bind(value) : new Result<TOut>(errors!);
    }

    /// <summary>Collapse the result to a single value by handling both cases.</summary>
    /// <typeparam name="TOut">The result of the match.</typeparam>
    /// <param name="onSuccess">Maps the success value.</param>
    /// <param name="onFailure">Maps the failures.</param>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(value) : onFailure(Errors);
    }

    /// <summary>Run one of two side-effecting actions depending on the outcome.</summary>
    /// <param name="onSuccess">Runs with the success value.</param>
    /// <param name="onFailure">Runs with the failures.</param>
    public void Switch(Action<T> onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
        {
            onSuccess(value);
        }
        else
        {
            onFailure(Errors);
        }
    }

    /// <summary>
    /// Fail a success whose value does not satisfy <paramref name="predicate"/>, with
    /// <paramref name="ifFalse"/>. A failure passes through unchanged.
    /// </summary>
    /// <param name="predicate">The condition the success value must satisfy.</param>
    /// <param name="ifFalse">The error to fail with when the predicate is false.</param>
    public Result<T> Ensure(Func<T, bool> predicate, Error ifFalse)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (IsFailure)
        {
            return this;
        }

        return predicate(value) ? this : new Result<T>([ifFalse]);
    }

    /// <summary>Run a side effect on the success value and return the result unchanged.</summary>
    /// <param name="onSuccess">The side effect.</param>
    public Result<T> Tap(Action<T> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        if (IsSuccess)
        {
            onSuccess(value);
        }

        return this;
    }

    /// <summary>The success value, or <paramref name="fallback"/> on failure.</summary>
    /// <param name="fallback">The value to return on failure.</param>
    public T OrElse(T fallback) => IsSuccess ? value : fallback;

    /// <summary>The success value, or the result of <paramref name="fallback"/> on failure.</summary>
    /// <param name="fallback">Produces a value from the failures.</param>
    public T OrElse(Func<IReadOnlyList<Error>, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return IsSuccess ? value : fallback(Errors);
    }

    /// <summary>Implicit success from a value.</summary>
    /// <param name="value">The success value.</param>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Implicit failure from a single error.</summary>
    /// <param name="error">The failure.</param>
    public static implicit operator Result<T>(Error error) => new([error]);

    /// <inheritdoc />
    public bool Equals(Result<T> other)
    {
        if (IsSuccess != other.IsSuccess)
        {
            return false;
        }

        return IsSuccess
            ? EqualityComparer<T>.Default.Equals(value, other.value)
            : Errors.SequenceEqual(other.Errors);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsSuccess ? HashCode.Combine(true, value) : HashCode.Combine(false, Errors.Count);

    /// <summary>Value equality.</summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <summary>Renders the outcome for logs.</summary>
    public override string ToString() =>
        IsSuccess ? $"Success({value})" : $"Failure({string.Join("; ", Errors)})";
}

/// <summary>Factories for <see cref="Result{T}"/>.</summary>
public static class Result
{
    /// <summary>A success carrying <paramref name="value"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The success value.</param>
    public static Result<T> Success<T>(T value) => new(value);

    /// <summary>A failure carrying <paramref name="error"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="error">The failure.</param>
    public static Result<T> Failure<T>(Error error) => new([error]);

    /// <summary>A failure carrying one or more <paramref name="errors"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="errors">The failures. Must be non-empty.</param>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result<T> Failure<T>(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var array = errors.ToArray();
        if (array.Length == 0)
        {
            throw new ArgumentException("A failure must carry at least one error.", nameof(errors));
        }

        return new Result<T>(array);
    }

    /// <summary>
    /// Run <paramref name="operation"/>, catching a boundary exception once and converting it to
    /// an <see cref="Error"/> via <paramref name="onException"/>. A deliberate, local escape hatch
    /// for interop with throwing APIs - not a substitute for returning results.
    /// <see cref="OperationCanceledException"/> always propagates.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The throwing operation.</param>
    /// <param name="onException">Maps a caught exception to an error.</param>
    public static Result<T> Try<T>(Func<T> operation, Func<Exception, Error> onException)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onException);
        try
        {
            return new Result<T>(operation());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // the whole point is to convert an arbitrary boundary fault to an Error
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new Result<T>([onException(ex)]);
        }
    }
}
