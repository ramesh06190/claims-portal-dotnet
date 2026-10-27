using ClaimsPortal.Core.Claims;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ClaimsPortal.Tests.Core;

public class ClaimServiceTests
{
    private readonly Mock<IClaimRepository> _repository = new();
    private readonly Mock<IClaimNumberGenerator> _numbers = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 27, 9, 30, 0, TimeSpan.Zero));
    private readonly ClaimService _service;

    public ClaimServiceTests()
    {
        _clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        _numbers.Setup(n => n.NewNumber()).Returns("CL-123456");
        _service = new ClaimService(_repository.Object, _numbers.Object, _clock);
    }

    private static SubmitClaimRequest Valid() => new()
    {
        ClaimantName = "  Avery Johnson  ",
        ClaimantId = "12345678",
        Email = " ",
        WeekEnding = new DateOnly(2026, 10, 24),
        WorkedThisWeek = false,
        AbleAndAvailable = true,
    };

    [Fact]
    public async Task Valid_claim_is_saved_with_trimmed_values_and_a_new_number()
    {
        var result = await _service.SubmitAsync(Valid());

        result.Succeeded.Should().BeTrue();
        result.Claim!.ClaimNumber.Should().Be("CL-123456");
        result.Claim.ClaimantName.Should().Be("Avery Johnson");
        result.Claim.Email.Should().BeNull();
        result.Claim.Status.Should().Be(ClaimStatus.PendingReview);
        result.Claim.SubmittedAt.Should().Be(_clock.GetUtcNow());
        _repository.Verify(r => r.AddAsync(result.Claim, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invalid_claim_is_not_saved()
    {
        var result = await _service.SubmitAsync(Valid() with { ClaimantName = "" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Field == "ClaimantName");
        _repository.Verify(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()), Times.Never);
        _numbers.Verify(n => n.NewNumber(), Times.Never);
    }

    [Fact]
    public async Task Claimant_not_able_to_work_needs_more_information()
    {
        var result = await _service.SubmitAsync(Valid() with { AbleAndAvailable = false });

        result.Claim!.Status.Should().Be(ClaimStatus.NeedsInformation);
    }

    [Fact]
    public async Task Earnings_are_ignored_when_the_claimant_did_not_work()
    {
        var result = await _service.SubmitAsync(Valid() with { WorkedThisWeek = false, GrossEarnings = 500 });

        result.Claim!.GrossEarnings.Should().BeNull();
        result.Claim.WeeklyAmount.Should().Be(ClaimService.BaseWeeklyBenefit);
    }

    [Theory]
    [InlineData(0, 370)]
    [InlineData(50, 370)]
    [InlineData(150, 270)]
    [InlineData(1000, 0)]
    public void Weekly_amount_reduces_dollar_for_dollar_above_the_disregard(decimal earnings, decimal expected)
    {
        ClaimService.CalculateWeeklyAmount(earnings).Should().Be(expected);
    }

    [Fact]
    public async Task Query_is_clamped_before_it_reaches_the_repository()
    {
        _repository
            .Setup(r => r.QueryAsync(It.IsAny<ClaimQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Claim>([], 1, ClaimQuery.MaxPageSize, 0));

        await _service.QueryAsync(new ClaimQuery { Page = -3, PageSize = 5000 });

        _repository.Verify(r => r.QueryAsync(
            It.Is<ClaimQuery>(q => q.Page == 1 && q.PageSize == ClaimQuery.MaxPageSize),
            It.IsAny<CancellationToken>()));
    }
}
