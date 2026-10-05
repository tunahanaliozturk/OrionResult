# OrionResult

One error-model vocabulary for .NET: a zero-alloc `Result<T>` struct, `Option<T>` and a structured `Error`, so expected failures (validation, not found, conflict) flow as data instead of exceptions.

![Railway composition: each step runs on the success track, a failure skips to Match](https://raw.githubusercontent.com/tunahanaliozturk/OrionResult/main/docs/diagrams/railway.png)

## Install

    dotnet add package OrionResult

Targets `net8.0`, `net9.0` and `net10.0`. Depends only on `Orion.Abstractions` 1.x.

## Quick start

```csharp
using Moongazing.OrionResult;

Result<User> FindUser(UserId id) =>
    repo.TryGet(id) is { } user
        ? user                                               // implicit T -> success
        : Error.NotFound("user.not_found", $"No user {id}"); // implicit Error -> failure

// Each step runs only if the previous one succeeded.
Result<ReceiptDto> Pay(UserId id, decimal amount) =>
    FindUser(id)
        .Ensure(u => u.IsActive, Error.Conflict("user.inactive", "User is deactivated"))
        .Bind(u => ChargeCard(u, amount))
        .Map(charge => new ReceiptDto(charge));

string message = Pay(id, 10m).Match(
    receipt => $"charged {receipt.Total}",
    errors  => $"failed: {errors[0].Code}");
```

## What is in the package

- `Result<T>`: a `readonly struct`. Success carries the value, failure carries one or more `Error`s. Combinators: `Map`, `Bind`, `Match`, `Switch`, `Ensure`, `Tap`, `OrElse`. Factories: `Result.Success`, `Result.Failure`, `Result.Try`.
- `Error`: a `readonly record struct` with `Code`, `Message`, `Kind`, optional `Fields` and `Extensions`. Factories `Error.Validation`, `NotFound`, `Conflict`, `FailedPrecondition`, `Unauthorized`, `Forbidden`, `RateLimited`, `Timeout`, `Unavailable`, `Unexpected`.
- `ErrorKind` (the failure category) and `FieldError` (`Field`, `Message`) for field-level validation detail.
- `Option<T>`: "maybe absent", distinct from "failed with a reason". `Map`, `Bind`, `Match`, `OrElse`, `ToResult(ifNone)`, plus `Option.From` and `FirstOrNone`.

## Behaviour

- Expected failures never throw. `Value` throws `InvalidOperationException` only when read on a failure (or on `None`); guard with `IsSuccess` or use `Match` / `OrElse`.
- `Result.Try(operation, onException)` turns an exception from a throwing API into an `Error`. `OperationCanceledException` is always rethrown.
- `default(Result<T>)` is a success carrying `default(T)`; `default(Option<T>)` is `None`.
- `Result<T>` implements `IOrionResult<T>` from `Orion.Abstractions`: `IOrionResult.Error` is the first `Error` mapped with `ToOrionError()`, and the first field name becomes `OrionError.Target`.
- AOT- and trim-compatible (`IsAotCompatible`), no reflection; CI publishes a NativeAOT smoke test.

## Related packages

- `Orion.Abstractions` - the shared contracts spine (`IOrionResult`, `OrionError`, telemetry, options, clock).

## Links

- Documentation and full README: https://github.com/tunahanaliozturk/OrionResult
- Changelog: https://github.com/tunahanaliozturk/OrionResult/blob/main/CHANGELOG.md
- License: MIT
