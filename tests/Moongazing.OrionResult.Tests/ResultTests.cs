namespace Moongazing.OrionResult.Tests;

using Moongazing.Orion.Abstractions.Results;

using Xunit;

public sealed class ResultTests
{
    [Fact]
    public void A_success_carries_its_value_and_reports_success()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void A_failure_carries_its_error_and_reports_failure()
    {
        Result<int> result = Error.NotFound("x.missing", "gone");

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal("x.missing", error.Code);
    }

    [Fact]
    public void Reading_the_value_of_a_failure_throws()
    {
        Result<int> result = Error.NotFound("x", "y");
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_transforms_a_success_and_propagates_a_failure()
    {
        Result<int> success = 21;
        Result<int> failure = Error.Conflict("c", "m");

        Assert.Equal(42, success.Map(x => x * 2).Value);
        Assert.True(failure.Map(x => x * 2).IsFailure);
        Assert.Equal("c", failure.Map(x => x * 2).Errors[0].Code);
    }

    [Fact]
    public void Bind_chains_a_success_and_short_circuits_a_failure()
    {
        static Result<int> Halve(int x) => x % 2 == 0 ? x / 2 : Error.Validation("odd", "not even");

        Assert.Equal(5, ((Result<int>)10).Bind(Halve).Value);
        Assert.True(((Result<int>)9).Bind(Halve).IsFailure);
        Assert.True(((Result<int>)Error.Conflict("c", "m")).Bind(Halve).IsFailure);
    }

    [Fact]
    public void Match_collapses_both_cases()
    {
        Result<int> success = 7;
        Result<int> failure = Error.NotFound("c", "m");

        Assert.Equal("ok:7", success.Match(v => $"ok:{v}", e => $"err:{e.Count}"));
        Assert.Equal("err:1", failure.Match(v => $"ok:{v}", e => $"err:{e.Count}"));
    }

    [Fact]
    public void Ensure_fails_a_success_that_breaks_the_predicate()
    {
        Result<int> result = 5;

        var kept = result.Ensure(x => x > 0, Error.Validation("neg", "must be positive"));
        var failed = result.Ensure(x => x > 10, Error.Validation("small", "too small"));

        Assert.True(kept.IsSuccess);
        Assert.True(failed.IsFailure);
        Assert.Equal("small", failed.Errors[0].Code);
    }

    [Fact]
    public void Ensure_passes_an_existing_failure_through_unchanged()
    {
        Result<int> failure = Error.Conflict("c", "m");
        var ensured = failure.Ensure(x => x > 0, Error.Validation("v", "v"));

        Assert.Equal("c", ensured.Errors[0].Code);
    }

    [Fact]
    public void Tap_runs_only_on_success_and_returns_the_result()
    {
        var log = new List<int>();
        Result<int> success = 3;
        Result<int> failure = Error.NotFound("c", "m");

        success.Tap(log.Add);
        failure.Tap(log.Add);

        Assert.Equal([3], log);
    }

    [Fact]
    public void OrElse_returns_the_fallback_on_failure()
    {
        Assert.Equal(1, ((Result<int>)1).OrElse(99));
        Assert.Equal(99, ((Result<int>)Error.NotFound("c", "m")).OrElse(99));
        Assert.Equal(1, ((Result<int>)Error.NotFound("c", "m")).OrElse(errors => errors.Count));
    }

    [Fact]
    public void A_railway_pipeline_short_circuits_at_the_first_failure()
    {
        static Result<int> Parse(string s) => int.TryParse(s, out var n) ? n : Error.Validation("nan", "not a number");

        var result = Parse("10")
            .Ensure(n => n > 0, Error.Validation("neg", "must be positive"))
            .Bind(n => Parse((n * 2).ToString(CultureInfo.InvariantCulture)))
            .Map(n => n + 1);

        Assert.Equal(21, result.Value);

        var broken = Parse("nope").Map(n => n + 1);
        Assert.True(broken.IsFailure);
        Assert.Equal("nan", broken.Errors[0].Code);
    }

    [Fact]
    public void It_implements_the_family_IOrionResult_contract()
    {
        IOrionResult<int> success = Result.Success(5);
        IOrionResult<int> failure = Result.Failure<int>(Error.NotFound("x.missing", "gone"));

        Assert.True(success.IsSuccess);
        Assert.Null(success.Error);
        Assert.Equal(5, success.Value);

        Assert.False(failure.IsSuccess);
        Assert.NotNull(failure.Error);
        Assert.Equal("x.missing", failure.Error!.Value.Code);
    }

    [Fact]
    public void Result_Failure_rejects_an_empty_error_list()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure<int>(Array.Empty<Error>()));
    }

    [Fact]
    public void Result_Try_converts_a_thrown_exception_to_an_error()
    {
        var result = Result.Try<int>(
            () => throw new InvalidOperationException("boom"),
            ex => Error.Unexpected("threw", ex.Message));

        Assert.True(result.IsFailure);
        Assert.Equal("threw", result.Errors[0].Code);
        Assert.Equal("boom", result.Errors[0].Message);
    }

    [Fact]
    public void Result_Try_returns_the_value_when_the_operation_succeeds()
    {
        Assert.Equal(7, Result.Try(() => 7, ex => Error.Unexpected("c", ex.Message)).Value);
    }

    [Fact]
    public void Result_Try_never_swallows_cancellation()
    {
        Assert.Throws<OperationCanceledException>(() =>
            Result.Try<int>(() => throw new OperationCanceledException(), ex => Error.Unexpected("c", "m")));
    }

    [Fact]
    public void Results_compare_by_value()
    {
        Assert.Equal((Result<int>)1, (Result<int>)1);
        Assert.NotEqual((Result<int>)1, (Result<int>)2);
        Assert.Equal(
            (Result<int>)Error.NotFound("c", "m"),
            (Result<int>)Error.NotFound("c", "m"));
        Assert.NotEqual((Result<int>)1, (Result<int>)Error.NotFound("c", "m"));
    }

    // Functor / monad laws (plan exit criterion).

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    public void Map_obeys_the_functor_identity_law(int x)
    {
        Result<int> r = x;
        Assert.Equal(r, r.Map(v => v));
    }

    [Theory]
    [InlineData(3)]
    public void Map_obeys_the_functor_composition_law(int x)
    {
        Result<int> r = x;
        Func<int, int> f = v => v + 1;
        Func<int, int> g = v => v * 2;

        Assert.Equal(r.Map(f).Map(g), r.Map(v => g(f(v))));
    }

    [Theory]
    [InlineData(5)]
    public void Bind_obeys_the_monad_left_identity_law(int x)
    {
        Func<int, Result<int>> f = v => v * 3;
        Assert.Equal(((Result<int>)x).Bind(f), f(x));
    }

    [Theory]
    [InlineData(5)]
    public void Bind_obeys_the_monad_right_identity_law(int x)
    {
        Result<int> r = x;
        Assert.Equal(r, r.Bind(v => (Result<int>)v));
    }
}
