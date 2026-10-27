using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimsPortal.Tests.Web;

/// <summary>Checks the accessibility contract of the server rendered form, not just that it works.</summary>
public sealed partial class ClaimFormPageTests(ClaimsPortalFactory factory) : IClassFixture<ClaimsPortalFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    private async Task<HttpResponseMessage> PostFormAsync(Dictionary<string, string> fields)
    {
        var page = await _client.GetStringAsync("/claims/new");
        fields["__RequestVerificationToken"] = AntiforgeryToken().Match(page).Groups[1].Value;
        using var content = new FormUrlEncodedContent(fields);
        return await _client.PostAsync("/claims/new", content);
    }

    [Fact]
    public async Task Form_without_antiforgery_token_is_rejected()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["ClaimantName"] = "x" });

        var response = await _client.PostAsync("/claims/new", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Errors_render_an_accessible_summary_and_mark_each_field()
    {
        var response = await PostFormAsync(new() { ["ClaimantId"] = "12ab", ["WeekEndingDay"] = "17", ["WeekEndingYear"] = "2026" });
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("<title>Error: File a weekly claim");
        html.Should().Contain("id=\"error-summary\" tabindex=\"-1\"");
        html.Should().Contain("<a href=\"#ClaimantName\">Enter your full name</a>");
        html.Should().Contain("<a href=\"#WeekEndingDay\">The week ending date must include a month</a>");
        html.Should().Contain("<a href=\"#WorkedThisWeek-yes\">Select yes if you worked during this week</a>");

        // What the person typed is kept, and the input is linked to its hint and its error
        html.Should().MatchRegex("<input[^>]*aria-describedby=\"ClaimantId-hint ClaimantId-error\"[^>]*aria-invalid=\"true\"|<input[^>]*aria-invalid=\"true\"[^>]*aria-describedby=\"ClaimantId-hint ClaimantId-error\"");
        html.Should().Contain("value=\"12ab\"");
        html.Should().Contain("<p class=\"field__error\" id=\"ClaimantId-error\">Error: Claimant ID must be 8 digits, like 12345678</p>");
    }

    [Fact]
    public async Task Valid_form_redirects_to_a_confirmation_page()
    {
        var response = await PostFormAsync(new()
        {
            ["ClaimantName"] = "Taylor Nguyen",
            ["ClaimantId"] = "11223344",
            ["Program"] = "UnemploymentInsurance",
            ["WeekEndingDay"] = "17",
            ["WeekEndingMonth"] = "10",
            ["WeekEndingYear"] = "2026",
            ["WorkedThisWeek"] = "no",
            ["AbleAndAvailable"] = "yes",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().MatchRegex("^/claims/CL-\\d{6}/submitted$");

        var confirmation = await _client.GetStringAsync(location);
        confirmation.Should().Contain("<h1>Claim submitted</h1>").And.Contain("Taylor Nguyen");
    }

    [Fact]
    public async Task Claims_list_marks_the_sorted_column_and_current_page()
    {
        var html = await _client.GetStringAsync("/claims?sort=Claimant&direction=Descending&page=2");

        html.Should().Contain("aria-sort=\"descending\"");
        Regex.Count(html, "aria-sort=").Should().Be(1, "only the sorted column has aria-sort");
        html.Should().Contain("aria-label=\"Page 2\" aria-current=\"page\"");
        html.Should().Contain("sorted by claimant descending");
    }
}
