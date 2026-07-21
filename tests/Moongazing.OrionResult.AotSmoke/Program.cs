// NativeAOT smoke test. Publishing this with PublishAot=true must produce zero trim/AOT warnings,
// and running it must exit 0 - OrionResult's AOT exit criterion. Runtime checks, not a framework.
using Moongazing.Orion.Abstractions.Results;
using Moongazing.OrionResult;

// Railway composition over a success.
Result<int> parsed = int.TryParse("21", out var n) ? n : Error.Validation("nan", "not a number");
var pipeline = parsed
    .Ensure(x => x > 0, Error.Validation("neg", "must be positive"))
    .Map(x => x * 2)
    .Bind(x => x < 100 ? Result.Success(x) : Error.Conflict("too_big", "over 100"));
Check(pipeline.IsSuccess && pipeline.Value == 42, "success pipeline wrong");

// Failure short-circuits and carries the error.
Result<int> failed = Error.NotFound("x.missing", "gone");
var mapped = failed.Map(x => x + 1);
Check(mapped.IsFailure && mapped.Errors[0].Code == "x.missing", "failure did not propagate");

// The family IOrionResult contract.
IOrionResult<int> asOrion = failed;
Check(!asOrion.IsSuccess && asOrion.Error is { } e && e.Code == "x.missing", "IOrionResult bridge wrong");

// Match collapses both cases.
var described = pipeline.Match(v => $"ok:{v}", errs => $"err:{errs.Count}");
Check(described == "ok:42", "match wrong");

// Option.
var numbers = new[] { 1, 2, 3 };
var found = numbers.FirstOrNone(x => x == 2);
Check(found.IsSome && found.Value == 2, "option find wrong");
Check(Option<int>.None.ToResult(Error.NotFound("c", "m")).IsFailure, "option->result wrong");

// Result.Try converts a boundary throw to an error.
var caught = Result.Try<int>(() => throw new InvalidOperationException("boom"), ex => Error.Unexpected("threw", ex.Message));
Check(caught.IsFailure && caught.Errors[0].Code == "threw", "try did not convert");

Console.WriteLine("OrionResult AOT smoke test passed.");
return 0;

static void Check(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine($"AOT smoke test failed: {message}");
        Environment.Exit(1);
    }
}
