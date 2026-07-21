<!-- markdownlint-disable MD024 -->

# Changelog

All notable changes to OrionResult are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.0] - 2026-07-21

The first release — the Orion family's Wave 1 result/error vocabulary.

### Added

- **`Result<T>`** — a zero-alloc `readonly struct` outcome: success carries a value, failure
  carries one or more `Error`s. Combinators `Map`, `Bind`, `Match`, `Switch`, `Ensure`, `Tap`,
  `OrElse`; implicit conversions from a value (success) and from an `Error` (failure); value
  equality. Implements the family's `IOrionResult<T>` (so `IOrionResult.Error` surfaces the first
  failure as an `OrionError`).
- **`Error`** — a structured `readonly record struct`: `Code`, `Message`, `ErrorKind`, optional
  `Fields` (field-level errors) and `Extensions`. Factory helpers (`Error.NotFound`,
  `Error.Validation`, `Error.Conflict`, …) and `ToOrionError()` for the spine bridge.
- **`ErrorKind`** and **`FieldError`** — the failure category and per-field detail.
- **`Option<T>`** — a zero-alloc "maybe absent" struct with `Some`/`None`, `Map`/`Bind`/`Match`/
  `OrElse`, `ToResult`, and the `FirstOrNone` / `From` helpers.
- **`Result.Try`** — a deliberate, local escape hatch that catches a boundary exception once and
  converts it to an `Error`; `OperationCanceledException` always propagates.
- Multi-targets `net8.0`/`net9.0`/`net10.0`; `IsAotCompatible`, no reflection in the core; a
  NativeAOT publish smoke test in CI that exercises the surface and runs the native binary with
  `-warnaserror`. Property tests assert `Map`/`Bind` obey the functor/monad identity and
  composition laws.

The RFC 9457 `ProblemDetails` / `ToHttpResult` mapping, async (`Task`/`ValueTask`) combinators,
and multi-error aggregation arrive in later waves.
