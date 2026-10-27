using ClaimsPortal.Core.Claims;
using FluentAssertions;

namespace ClaimsPortal.Tests.Core;

public class ClaimValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);
    private static readonly DateOnly LastSaturday = new(2026, 10, 24);

    private static SubmitClaimRequest Valid() => new()
    {
        ClaimantName = "Avery Johnson",
        ClaimantId = "12345678",
        Email = "avery@example.com",
        WeekEnding = LastSaturday,
        WorkedThisWeek = false,
        AbleAndAvailable = true,
    };

    [Fact]
    public void Valid_request_has_no_errors()
    {
        ClaimValidator.Validate(Valid(), Today).Should().BeEmpty();
    }

    [Fact]
    public void Empty_request_reports_every_required_field_with_a_fix_it_message()
    {
        var errors = ClaimValidator.Validate(new SubmitClaimRequest(), Today);

        errors.Should().BeEquivalentTo(
        [
            new ValidationError("ClaimantName", "Enter your full name"),
            new ValidationError("ClaimantId", "Enter your claimant ID"),
            new ValidationError("WeekEnding", "Enter the week ending date"),
            new ValidationError("WorkedThisWeek", "Select yes if you worked during this week"),
            new ValidationError("AbleAndAvailable", "Select yes if you were able and available for work"),
        ], o => o.WithStrictOrdering());
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234abcd")]
    public void Claimant_id_must_be_eight_digits(string claimantId)
    {
        var errors = ClaimValidator.Validate(Valid() with { ClaimantId = claimantId }, Today);

        errors.Should().ContainSingle().Which.Message.Should().Be("Claimant ID must be 8 digits, like 12345678");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("name@localhost")]
    [InlineData("Avery <avery@example.com>")]
    public void Email_is_optional_but_must_be_valid_when_given(string email)
    {
        ClaimValidator.Validate(Valid() with { Email = email }, Today)
            .Should().ContainSingle(e => e.Field == "Email");

        ClaimValidator.Validate(Valid() with { Email = "  " }, Today).Should().BeEmpty();
    }

    [Fact]
    public void Week_ending_cannot_be_in_the_future()
    {
        var errors = ClaimValidator.Validate(Valid() with { WeekEnding = new DateOnly(2026, 10, 31) }, Today);

        errors.Should().ContainSingle().Which.Message.Should().Be("The week ending date must be today or in the past");
    }

    [Fact]
    public void Week_ending_must_be_a_saturday()
    {
        var friday = LastSaturday.AddDays(-1);

        ClaimValidator.Validate(Valid() with { WeekEnding = friday }, Today)
            .Should().ContainSingle().Which.Message.Should().Be("The week ending date must be a Saturday");
    }

    [Fact]
    public void Earnings_are_required_when_the_claimant_worked()
    {
        var errors = ClaimValidator.Validate(Valid() with { WorkedThisWeek = true }, Today);

        errors.Should().ContainSingle().Which.Should().Be(
            new ValidationError("GrossEarnings", "Enter your gross earnings for this week"));
    }

    [Fact]
    public void Earnings_cannot_be_negative()
    {
        ClaimValidator.Validate(Valid() with { WorkedThisWeek = true, GrossEarnings = -1 }, Today)
            .Should().ContainSingle().Which.Message.Should().Be("Gross earnings must be 0 or more");
    }
}
