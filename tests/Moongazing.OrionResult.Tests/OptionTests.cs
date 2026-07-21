namespace Moongazing.OrionResult.Tests;

using Xunit;

public sealed class OptionTests
{
    [Fact]
    public void Some_carries_a_value()
    {
        var option = Option<int>.Some(5);

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
        Assert.Equal(5, option.Value);
    }

    [Fact]
    public void None_has_no_value_and_reading_it_throws()
    {
        var option = Option<int>.None;

        Assert.True(option.IsNone);
        Assert.Throws<InvalidOperationException>(() => option.Value);
    }

    [Fact]
    public void The_default_option_is_none()
    {
        Assert.True(default(Option<int>).IsNone);
    }

    [Fact]
    public void Map_transforms_some_and_propagates_none()
    {
        Assert.Equal(10, Option<int>.Some(5).Map(x => x * 2).Value);
        Assert.True(Option<int>.None.Map(x => x * 2).IsNone);
    }

    [Fact]
    public void Bind_chains_some_and_short_circuits_none()
    {
        static Option<int> Positive(int x) => x > 0 ? Option<int>.Some(x) : Option<int>.None;

        Assert.Equal(3, Option<int>.Some(3).Bind(Positive).Value);
        Assert.True(Option<int>.Some(-1).Bind(Positive).IsNone);
        Assert.True(Option<int>.None.Bind(Positive).IsNone);
    }

    [Fact]
    public void Match_collapses_both_cases()
    {
        Assert.Equal("some:4", Option<int>.Some(4).Match(v => $"some:{v}", () => "none"));
        Assert.Equal("none", Option<int>.None.Match(v => $"some:{v}", () => "none"));
    }

    [Fact]
    public void OrElse_supplies_a_fallback_for_none()
    {
        Assert.Equal(7, Option<int>.Some(7).OrElse(99));
        Assert.Equal(99, Option<int>.None.OrElse(99));
        Assert.Equal(99, Option<int>.None.OrElse(() => 99));
    }

    [Fact]
    public void ToResult_turns_none_into_a_failure()
    {
        var some = Option<int>.Some(3).ToResult(Error.NotFound("c", "m"));
        var none = Option<int>.None.ToResult(Error.NotFound("x.missing", "gone"));

        Assert.True(some.IsSuccess);
        Assert.Equal(3, some.Value);
        Assert.True(none.IsFailure);
        Assert.Equal("x.missing", none.Errors[0].Code);
    }

    [Fact]
    public void An_implicit_value_becomes_some()
    {
        Option<int> option = 8;
        Assert.True(option.IsSome);
        Assert.Equal(8, option.Value);
    }

    [Fact]
    public void FirstOrNone_finds_a_match_or_returns_none()
    {
        var numbers = new[] { 1, 2, 3, 4 };

        Assert.Equal(2, numbers.FirstOrNone(x => x % 2 == 0).Value);
        Assert.True(numbers.FirstOrNone(x => x > 10).IsNone);
    }

    [Fact]
    public void From_maps_null_to_none()
    {
        Assert.True(Option.From<string>(null).IsNone);
        Assert.Equal("x", Option.From("x").Value);
    }

    [Fact]
    public void Options_compare_by_value()
    {
        Assert.Equal(Option<int>.Some(1), Option<int>.Some(1));
        Assert.NotEqual(Option<int>.Some(1), Option<int>.Some(2));
        Assert.Equal(Option<int>.None, Option<int>.None);
        Assert.NotEqual(Option<int>.Some(1), Option<int>.None);
    }
}
