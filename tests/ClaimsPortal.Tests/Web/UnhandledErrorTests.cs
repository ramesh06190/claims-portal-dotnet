using System.Net;
using System.Text.Json;
using ClaimsPortal.Core.Claims;
using ClaimsPortal.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ClaimsPortal.Tests.Web;

/// <summary>
/// What callers see when something fails that nobody planned for, here a database that throws.
/// Neither the API nor the pages may leak the exception message or a stack trace.
/// </summary>
public sealed class UnhandledErrorTests : IClassFixture<ClaimsPortalFactory>
{
    private const string SecretDetail = "Login failed for user 'claims_app' on server sql-prod-01";

    private readonly HttpClient _client;

    public UnhandledErrorTests(ClaimsPortalFactory factory)
    {
        var failingRepository = new Mock<IClaimRepository>();
        failingRepository
            .Setup(r => r.QueryAsync(It.IsAny<ClaimQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(SecretDetail));

        _client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped(_ => failingRepository.Object)))
            .CreateClient();
    }

    [Fact]
    public async Task Api_returns_a_500_problem_with_the_correlation_id_and_no_exception_details()
    {
        var response = await _client.GetAsync("/api/claims");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(SecretDetail).And.NotContain("InvalidOperationException").And.NotContain(" at ");

        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().Should().Be("Something went wrong on our side");
        problem.GetProperty("correlationId").GetString()
            .Should().Be(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Pages_show_the_friendly_error_page_with_a_reference()
    {
        var response = await _client.GetAsync("/claims");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Sorry, something went wrong").And.NotContain(SecretDetail);
    }
}
