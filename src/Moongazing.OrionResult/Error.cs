namespace Moongazing.OrionResult;

using Moongazing.Orion.Abstractions.Results;

/// <summary>
/// A structured, expected failure: a machine-readable <see cref="Code"/>, a human-readable
/// <see cref="Message"/>, a <see cref="ErrorKind"/> category, and optionally per-field errors
/// and extension data. This is the value a <see cref="Result{T}"/> carries when it fails - not
/// an exception and not a free-text string.
/// </summary>
/// <remarks>
/// Bridges to the family's <see cref="OrionError"/> via <see cref="ToOrionError"/>, so an Orion
/// result maps to the shared error vocabulary (and, in a later wave, to RFC 9457
/// <c>ProblemDetails</c>) exactly once.
/// </remarks>
public readonly record struct Error
{
    /// <summary>Create a structured error.</summary>
    /// <param name="code">Stable machine-readable code, e.g. <c>user.not_found</c>. Not localised.</param>
    /// <param name="message">Human-readable description, safe to log.</param>
    /// <param name="kind">The failure category.</param>
    /// <param name="fields">Optional per-field failures (for <see cref="ErrorKind.Validation"/>).</param>
    /// <param name="extensions">Optional extension data carried alongside the error.</param>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is blank.</exception>
    public Error(
        string code,
        string message,
        ErrorKind kind = ErrorKind.Unexpected,
        IReadOnlyList<FieldError>? fields = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Kind = kind;
        Fields = fields;
        Extensions = extensions;
    }

    /// <summary>The stable machine-readable code consumers branch on.</summary>
    public string Code { get; }

    /// <summary>The human-readable description. Safe to log; not a UI string.</summary>
    public string Message { get; }

    /// <summary>The failure category.</summary>
    public ErrorKind Kind { get; }

    /// <summary>Per-field failures, or null. Populated for validation errors.</summary>
    public IReadOnlyList<FieldError>? Fields { get; }

    /// <summary>Extension data carried alongside the error, or null.</summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>An <see cref="ErrorKind.Validation"/> error, optionally with field-level detail.</summary>
    public static Error Validation(string code, string message, IReadOnlyList<FieldError>? fields = null) =>
        new(code, message, ErrorKind.Validation, fields);

    /// <summary>A <see cref="ErrorKind.NotFound"/> error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    /// <summary>A <see cref="ErrorKind.Conflict"/> error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    /// <summary>A <see cref="ErrorKind.FailedPrecondition"/> error.</summary>
    public static Error FailedPrecondition(string code, string message) =>
        new(code, message, ErrorKind.FailedPrecondition);

    /// <summary>An <see cref="ErrorKind.Unauthorized"/> error.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorKind.Unauthorized);

    /// <summary>A <see cref="ErrorKind.Forbidden"/> error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorKind.Forbidden);

    /// <summary>A <see cref="ErrorKind.RateLimited"/> error.</summary>
    public static Error RateLimited(string code, string message) => new(code, message, ErrorKind.RateLimited);

    /// <summary>A <see cref="ErrorKind.Timeout"/> error.</summary>
    public static Error Timeout(string code, string message) => new(code, message, ErrorKind.Timeout);

    /// <summary>A <see cref="ErrorKind.Unavailable"/> error.</summary>
    public static Error Unavailable(string code, string message) => new(code, message, ErrorKind.Unavailable);

    /// <summary>An <see cref="ErrorKind.Unexpected"/> error.</summary>
    public static Error Unexpected(string code, string message) => new(code, message, ErrorKind.Unexpected);

    /// <summary>
    /// Map to the family's <see cref="OrionError"/>. The <see cref="Code"/> and <see cref="Message"/>
    /// carry over; the first field name (if any) becomes the <see cref="OrionError.Target"/>.
    /// </summary>
    public OrionError ToOrionError() =>
        new(Code, Message, Fields is { Count: > 0 } ? Fields[0].Field : null);

    /// <summary>Renders as <c>{Kind}/{Code}: {Message}</c>.</summary>
    public override string ToString() => $"{Kind}/{Code}: {Message}";
}
