namespace Moongazing.OrionResult.Tests;

using Moongazing.Orion.Abstractions.Results;

using Xunit;

public sealed class ErrorTests
{
    [Fact]
    public void An_error_carries_code_message_and_kind()
    {
        var error = new Error("user.not_found", "No such user", ErrorKind.NotFound);

        Assert.Equal("user.not_found", error.Code);
        Assert.Equal("No such user", error.Message);
        Assert.Equal(ErrorKind.NotFound, error.Kind);
        Assert.Null(error.Fields);
    }

    [Theory]
    [InlineData(null, "m")]
    [InlineData("", "m")]
    [InlineData("  ", "m")]
    [InlineData("c", null)]
    [InlineData("c", "")]
    public void A_blank_code_or_message_is_rejected(string? code, string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code!, message!));
    }

    [Fact]
    public void The_factories_set_the_matching_kind()
    {
        Assert.Equal(ErrorKind.Validation, Error.Validation("c", "m").Kind);
        Assert.Equal(ErrorKind.NotFound, Error.NotFound("c", "m").Kind);
        Assert.Equal(ErrorKind.Conflict, Error.Conflict("c", "m").Kind);
        Assert.Equal(ErrorKind.FailedPrecondition, Error.FailedPrecondition("c", "m").Kind);
        Assert.Equal(ErrorKind.Unauthorized, Error.Unauthorized("c", "m").Kind);
        Assert.Equal(ErrorKind.Forbidden, Error.Forbidden("c", "m").Kind);
        Assert.Equal(ErrorKind.RateLimited, Error.RateLimited("c", "m").Kind);
        Assert.Equal(ErrorKind.Timeout, Error.Timeout("c", "m").Kind);
        Assert.Equal(ErrorKind.Unavailable, Error.Unavailable("c", "m").Kind);
        Assert.Equal(ErrorKind.Unexpected, Error.Unexpected("c", "m").Kind);
    }

    [Fact]
    public void A_validation_error_carries_its_field_errors()
    {
        var fields = new[] { new FieldError("email", "required"), new FieldError("age", "must be positive") };

        var error = Error.Validation("bad_request", "Validation failed", fields);

        Assert.Equal(fields, error.Fields);
    }

    [Fact]
    public void ToOrionError_maps_code_message_and_the_first_field_as_target()
    {
        var error = Error.Validation("bad", "nope", [new FieldError("email", "required")]);

        OrionError orion = error.ToOrionError();

        Assert.Equal("bad", orion.Code);
        Assert.Equal("nope", orion.Message);
        Assert.Equal("email", orion.Target);
    }

    [Fact]
    public void ToOrionError_leaves_target_null_when_there_are_no_fields()
    {
        Assert.Null(Error.NotFound("c", "m").ToOrionError().Target);
    }

    [Fact]
    public void Errors_compare_by_value()
    {
        var a = Error.NotFound("c", "m");
        var b = Error.NotFound("c", "m");
        var c = Error.Conflict("c", "m");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
