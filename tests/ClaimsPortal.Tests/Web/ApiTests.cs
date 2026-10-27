using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimsPortal.Web.Middleware;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPortal.Tests.Web;

public sealed class ApiTests(ClaimsPortalFactory factory) : IClassFixture<ClaimsPortalFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_pages_and_sorts()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/claims?pageSize=5&sort=Claimant&direction=Ascending");

        json.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(24);
        json.GetProperty("pageSize").GetInt32().Should().Be(5);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(5);
        items[0].GetProperty("claimantName").GetString().Should().Be("Avery Johnson");
        items[0].GetProperty("status").ValueKind.Should().Be(JsonValueKind.String, "enums are sent as names, not numbers");
    }

    [Fact]
    public async Task List_does_not_expose_contact_details_or_claimant_ids()
    {
        var body = await _client.GetStringAsync("/api/claims?pageSize=50");

        body.Should().NotContain("claimantId").And.NotContain("email").And.NotContain("phone");
    }

    [Fact]
    public async Task Unknown_claim_returns_problem_details_404()
    {
        var response = await _client.GetAsync("/api/claims/CL-000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Invalid_claim_returns_validation_problem_with_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/claims", new { claimantId = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        errors.GetProperty("claimantName")[0].GetString().Should().Be("Enter your full name");
        errors.GetProperty("claimantId")[0].GetString().Should().Be("Claimant ID must be 8 digits, like 12345678");
    }

    [Fact]
    public async Task Valid_claim_is_created_with_a_location_and_an_outbox_message()
    {
        var response = await _client.PostAsJsonAsync("/api/claims", new
        {
            claimantName = "Jordan Smith",
            claimantId = "87654321",
            program = "WorkShare",
            weekEnding = "2026-10-17",
            workedThisWeek = true,
            grossEarnings = 150,
            ableAndAvailable = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var number = created.GetProperty("claimNumber").GetString()!;
        created.GetProperty("weeklyAmount").GetDecimal().Should().Be(270);
        response.Headers.Location!.ToString().Should().EndWith($"/api/claims/{number}");

        await using var db = factory.CreateDbContext();
        (await db.OutboxMessages.AnyAsync(m => m.Payload.Contains(number))).Should().BeTrue();
    }

    [Fact]
    public async Task Every_response_has_security_headers_and_a_correlation_id()
    {
        var response = await _client.GetAsync("/");

        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("script-src 'self'");
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Single().Should().Be("DENY");
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single().Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Theory]
    [InlineData("abc-123", "abc-123")]
    [InlineData("bad value\r\ninjected", null)]
    public async Task Correlation_id_is_reused_only_when_safe(string sent, string? expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/claims?pageSize=1");
        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, sent);

        var response = await _client.SendAsync(request);
        var echoed = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();

        if (expected is null)
        {
            echoed.Should().NotBe(sent).And.MatchRegex("^[0-9a-f]{32}$");
        }
        else
        {
            echoed.Should().Be(expected);
        }
    }
}
