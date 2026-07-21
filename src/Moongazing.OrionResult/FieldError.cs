namespace Moongazing.OrionResult;

/// <summary>
/// A single field-level failure inside a <see cref="ErrorKind.Validation"/> <see cref="Error"/> -
/// which input was rejected and why. Carried in <see cref="Error.Fields"/> so a validation
/// failure renders as an <c>errors[]</c> array (RFC 9457) rather than a flat string.
/// </summary>
/// <param name="Field">The name of the offending field, e.g. <c>email</c> or <c>items[0].qty</c>.</param>
/// <param name="Message">The human-readable reason the field was rejected.</param>
public readonly record struct FieldError(string Field, string Message);
