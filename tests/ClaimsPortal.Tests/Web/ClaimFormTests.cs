using ClaimsPortal.Web.Forms;
using FluentAssertions;

namespace ClaimsPortal.Tests.Web;

public class ClaimFormTests
{
    [Theory]
    [InlineData("24", "10", "2026", 2026, 10, 24)]
    [InlineData(" 4 ", "07", "1988", 1988, 7, 4)]
    [InlineData("29", "2", "2024", 2024, 2, 29)]
    public void Memorable_date_parses_real_dates(string d, string m, string y, int year, int month, int day)
    {
        MemorableDate.Parse(d, m, y, "date", out var error).Should().Be(new DateOnly(year, month, day));
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("17", "", "2026", "The week ending date must include a month")]
    [InlineData("", "", "2026", "The week ending date must include a day and month")]
    [InlineData("31", "2", "2026", "The week ending date must be a real date")]
    [InlineData("29", "2", "2026", "The week ending date must be a real date")]
    [InlineData("1", "13", "2026", "The week ending date must be a real date")]
    [InlineData("1", "1", "26", "The week ending date must be a real date")]
    [InlineData("a", "1", "2026", "The week ending date must be a real date")]
    public void Memorable_date_names_the_problem(string d, string m, string y, string expected)
    {
        MemorableDate.Parse(d, m, y, "week ending date", out var error).Should().BeNull();
        error.Should().Be(expected);
    }

    [Fact]
    public void Empty_date_is_not_a_format_error_so_the_required_message_is_shown_instead()
    {
        MemorableDate.Parse(null, " ", "", "date", out var error).Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void Form_maps_yes_no_answers_and_money()
    {
        var form = new ClaimForm
        {
            WeekEndingDay = "24", WeekEndingMonth = "10", WeekEndingYear = "2026",
            WorkedThisWeek = "yes", AbleAndAvailable = "no", GrossEarnings = "$1,250.50",
        };

        var (request, errors) = form.ToRequest();

        errors.Should().BeEmpty();
        request.WorkedThisWeek.Should().BeTrue();
        request.AbleAndAvailable.Should().BeFalse();
        request.GrossEarnings.Should().Be(1250.50m);
        request.WeekEnding.Should().Be(new DateOnly(2026, 10, 24));
    }

    [Fact]
    public void Form_reports_earnings_that_are_not_a_number()
    {
        var (_, errors) = new ClaimForm { GrossEarnings = "about 100" }.ToRequest();

        errors.Should().ContainSingle().Which.Message.Should().Be("Gross earnings must be a number, like 125.50");
    }
}
