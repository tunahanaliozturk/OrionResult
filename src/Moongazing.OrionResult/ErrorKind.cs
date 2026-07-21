namespace Moongazing.OrionResult;

/// <summary>
/// The category of an <see cref="Error"/> - the low-cardinality dimension that (in a later wave)
/// maps to an HTTP status and lets a caller branch on <em>what kind</em> of failure occurred
/// without string-matching the <see cref="Error.Code"/>. Mirrors the canonical vocabulary in
/// <see cref="Moongazing.Orion.Abstractions.Results.OrionErrorCodes"/>.
/// </summary>
public enum ErrorKind
{
    /// <summary>An argument or input was rejected before any work was attempted.</summary>
    Validation,

    /// <summary>The addressed entity does not exist.</summary>
    NotFound,

    /// <summary>The entity already exists, or the operation lost a race for it.</summary>
    Conflict,

    /// <summary>The operation could not run because the entity was in the wrong state.</summary>
    FailedPrecondition,

    /// <summary>The caller could not be identified.</summary>
    Unauthorized,

    /// <summary>The caller was identified but is not allowed to do this.</summary>
    Forbidden,

    /// <summary>The caller exceeded a quota or rate limit.</summary>
    RateLimited,

    /// <summary>The operation exceeded its deadline.</summary>
    Timeout,

    /// <summary>A dependency was unreachable or unhealthy; retrying may succeed.</summary>
    Unavailable,

    /// <summary>An unexpected fault - the fallback when nothing more specific fits.</summary>
    Unexpected,
}
