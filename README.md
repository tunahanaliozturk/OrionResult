<p align="center">
  <img src="docs/logo.png" alt="OrionResult" width="150" />
</p>

# OrionResult

[![CI/CD](https://github.com/tunahanaliozturk/OrionResult/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/tunahanaliozturk/OrionResult/actions/workflows/ci-cd.yml)
[![NuGet](https://img.shields.io/nuget/v/OrionResult.svg)](https://www.nuget.org/packages/OrionResult/)

One error-model vocabulary for the **Orion** family. Most "failures" in real code are *expected* — validation rejected the input, the resource wasn't found, the caller isn't authorized — and modelling those as exceptions gives you invisible control flow, stack-trace tax, and forty different `catch` shapes at the HTTP boundary. So teams reach for a Result type, and now there are five of them, none agreeing on how a failure becomes a response.

OrionResult gives the family **one** `Result`/`Option`/`Error` vocabulary: a zero-alloc `Result<T>` struct, a structured `Error` (stable code + kind, not free-text), and the family's `IOrionResult` contract — so failures flow as data, not as control flow.

## Features

- **`Result<T>`** — a `readonly struct`, so the success path allocates nothing. Success carries the value; failure carries one or more `Error`s. Railway composition with `Map`, `Bind`, `Match`, `Ensure`, `Tap`, `OrElse`.
- **Structured `Error`** — a machine-readable `Code`, a human `Message`, an `ErrorKind` category, and optional field-level errors and extensions. Factory helpers (`Error.NotFound`, `Error.Validation`, …). Bridges to the spine's `OrionError`.
- **`Option<T>`** — "maybe absent", distinct from "failed with a reason". `Map`/`Bind`/`Match`/`OrElse`, `ToResult`, `FirstOrNone`.
- **Implicit conversions** keep call sites clean: `return value;` is a success, `return Error.NotFound(...);` is a failure.
- **Implements `IOrionResult<T>`** — cross-cutting code (and, in a later wave, the RFC 9457 `ProblemDetails` bridge) reads any Orion result the same way.
- **AOT- and trim-clean**, verified by a native-binary smoke test in CI; no reflection in the core. Multi-targets `net8.0`, `net9.0`, `net10.0`.

## Install

```bash
dotnet add package OrionResult
```

## Quick start

```csharp
using Moongazing.OrionResult;

Result<User> FindUser(UserId id) =>
    repo.TryGet(id) is { } user
        ? user                                          // implicit T -> success
        : Error.NotFound("user.not_found", $"No user {id}"); // implicit Error -> failure

// Railway composition: each step runs only if the previous succeeded.
Result<ReceiptDto> Pay(UserId id, decimal amount) =>
    FindUser(id)
        .Ensure(u => u.IsActive, Error.Conflict("user.inactive", "User is deactivated"))
        .Bind(u => ChargeCard(u, amount))
        .Map(charge => new ReceiptDto(charge));

// Collapse to a value by handling both cases.
string message = Pay(id, 10m).Match(
    receipt => $"charged {receipt.Total}",
    errors  => $"failed: {errors[0].Code}");
```

`Option<T>` for "maybe absent":

```csharp
Option<Email> primary = user.Emails.FirstOrNone(e => e.IsPrimary);
Email best = primary.OrElse(Email.Empty);

// Turn absence into a typed failure when you need a Result.
Result<Email> required = primary.ToResult(Error.Validation("email.required", "A primary email is required"));
```

Catch a boundary exception once and convert it — a deliberate, local escape hatch:

```csharp
Result<Config> parsed = Result.Try(
    () => Config.Parse(raw),
    ex => Error.Validation("config.invalid", ex.Message));
```

## Versioning

Follows [Semantic Versioning](https://semver.org/). Multi-targets `net8.0`, `net9.0`, and `net10.0`. Binds to `Orion.Abstractions` 1.x. The RFC 9457 `ProblemDetails` / `ToHttpResult` HTTP mapping and async combinators arrive in later waves; this release is the core value types.

## Documentation

- [CHANGELOG.md](CHANGELOG.md) — release notes.

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) and the [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## License

[MIT](LICENSE).
