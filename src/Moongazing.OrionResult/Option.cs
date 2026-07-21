namespace Moongazing.OrionResult;

using System.Collections.Generic;

/// <summary>
/// A value that may be absent - <see cref="Some"/> carrying a <typeparamref name="T"/>, or
/// <see cref="None"/>. Distinct from <see cref="Result{T}"/>: an option models "maybe there",
/// not "failed with a reason". A <c>readonly struct</c>, so the present case allocates nothing.
/// </summary>
/// <typeparam name="T">The contained value type.</typeparam>
/// <remarks><c>default(Option&lt;T&gt;)</c> is <see cref="None"/>.</remarks>
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T value;

    private Option(T value)
    {
        this.value = value;
        IsSome = true;
    }

    /// <summary>True when a value is present.</summary>
    public bool IsSome { get; }

    /// <summary>True when no value is present.</summary>
    public bool IsNone => !IsSome;

    /// <summary>An option carrying <paramref name="value"/>.</summary>
    /// <param name="value">The present value.</param>
    public static Option<T> Some(T value) => new(value);

    /// <summary>The absent option.</summary>
    public static Option<T> None => default;

    /// <summary>
    /// The value. Throws <see cref="InvalidOperationException"/> when <see cref="IsNone"/> -
    /// guard with <see cref="IsSome"/> or use <see cref="Match{TOut}"/>/<see cref="OrElse(T)"/>.
    /// </summary>
    public T Value => IsSome
        ? value
        : throw new InvalidOperationException("Cannot read the value of a None option.");

    /// <summary>Transform a present value, propagating <see cref="None"/> unchanged.</summary>
    /// <typeparam name="TOut">The mapped type.</typeparam>
    /// <param name="map">The transform.</param>
    public Option<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSome ? Option<TOut>.Some(map(value)) : Option<TOut>.None;
    }

    /// <summary>Chain another option-producing step, propagating <see cref="None"/> unchanged.</summary>
    /// <typeparam name="TOut">The next option's type.</typeparam>
    /// <param name="bind">The next step.</param>
    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSome ? bind(value) : Option<TOut>.None;
    }

    /// <summary>Collapse to a single value by handling both cases.</summary>
    /// <typeparam name="TOut">The result type.</typeparam>
    /// <param name="onSome">Maps the present value.</param>
    /// <param name="onNone">Produces a value for the absent case.</param>
    public TOut Match<TOut>(Func<T, TOut> onSome, Func<TOut> onNone)
    {
        ArgumentNullException.ThrowIfNull(onSome);
        ArgumentNullException.ThrowIfNull(onNone);
        return IsSome ? onSome(value) : onNone();
    }

    /// <summary>The value, or <paramref name="fallback"/> when absent.</summary>
    /// <param name="fallback">The fallback value.</param>
    public T OrElse(T fallback) => IsSome ? value : fallback;

    /// <summary>The value, or the result of <paramref name="fallback"/> when absent.</summary>
    /// <param name="fallback">Produces a fallback value.</param>
    public T OrElse(Func<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return IsSome ? value : fallback();
    }

    /// <summary>
    /// Turn an absent option into a failed <see cref="Result{T}"/> with <paramref name="ifNone"/>;
    /// a present option becomes a success.
    /// </summary>
    /// <param name="ifNone">The error to use when the option is <see cref="None"/>.</param>
    public Result<T> ToResult(Error ifNone) => IsSome ? Result.Success(value) : Result.Failure<T>(ifNone);

    /// <summary>Implicit <see cref="Some"/> from a value.</summary>
    /// <param name="value">The present value.</param>
    public static implicit operator Option<T>(T value) => Some(value);

    /// <inheritdoc />
    public bool Equals(Option<T> other) =>
        IsSome == other.IsSome && (!IsSome || EqualityComparer<T>.Default.Equals(value, other.value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsSome ? HashCode.Combine(true, value) : 0;

    /// <summary>Value equality.</summary>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    /// <summary>Renders as <c>Some({Value})</c> or <c>None</c>.</summary>
    public override string ToString() => IsSome ? $"Some({value})" : "None";
}

/// <summary>Factories and LINQ-style helpers for <see cref="Option{T}"/>.</summary>
public static class Option
{
    /// <summary>An option carrying <paramref name="value"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The present value.</param>
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    /// <summary>The absent option of <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static Option<T> None<T>() => Option<T>.None;

    /// <summary>An option from a possibly-null reference: <see cref="Option{T}.None"/> when null.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The possibly-null value.</param>
    public static Option<T> From<T>(T? value)
        where T : class => value is null ? Option<T>.None : Option<T>.Some(value);

    /// <summary>The first element matching <paramref name="predicate"/>, or <see cref="Option{T}.None"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The match.</param>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        foreach (var item in source)
        {
            if (predicate(item))
            {
                return Option<T>.Some(item);
            }
        }

        return Option<T>.None;
    }
}
