namespace ClaimsPortal.Core.Claims;

public sealed record SubmitClaimResult(Claim? Claim, IReadOnlyList<ValidationError> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

public sealed class ClaimService(
    IClaimRepository repository,
    IClaimNumberGenerator numberGenerator,
    TimeProvider timeProvider)
{
    /// <summary>Base weekly benefit before partial earnings are deducted.</summary>
    public const decimal BaseWeeklyBenefit = 370m;

    /// <summary>Earnings up to this amount do not reduce the benefit.</summary>
    public const decimal EarningsDisregard = 50m;

    public Task<PagedResult<Claim>> QueryAsync(ClaimQuery query, CancellationToken cancellationToken = default) =>
        repository.QueryAsync(query.Normalize(), cancellationToken);

    public Task<Claim?> GetAsync(string claimNumber, CancellationToken cancellationToken = default) =>
        repository.GetByNumberAsync(claimNumber, cancellationToken);

    public async Task<SubmitClaimResult> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetLocalNow();
        var errors = ClaimValidator.Validate(request, DateOnly.FromDateTime(now.DateTime));
        if (errors.Count > 0)
        {
            return new SubmitClaimResult(null, errors);
        }

        var earnings = request.WorkedThisWeek == true ? request.GrossEarnings ?? 0 : 0;
        var claim = new Claim
        {
            ClaimNumber = numberGenerator.NewNumber(),
            ClaimantName = request.ClaimantName!.Trim(),
            ClaimantId = request.ClaimantId!.Trim(),
            Email = NullIfBlank(request.Email),
            Phone = NullIfBlank(request.Phone),
            Program = request.Program,
            WeekEnding = request.WeekEnding!.Value,
            WorkedThisWeek = request.WorkedThisWeek!.Value,
            GrossEarnings = request.WorkedThisWeek == true ? earnings : null,
            AbleAndAvailable = request.AbleAndAvailable!.Value,
            WeeklyAmount = CalculateWeeklyAmount(earnings),
            Status = request.AbleAndAvailable == true ? ClaimStatus.PendingReview : ClaimStatus.NeedsInformation,
            SubmittedAt = now,
        };

        await repository.AddAsync(claim, cancellationToken);
        return new SubmitClaimResult(claim, []);
    }

    /// <summary>Every dollar earned above the disregard reduces the benefit by a dollar.</summary>
    public static decimal CalculateWeeklyAmount(decimal grossEarnings) =>
        Math.Max(0, BaseWeeklyBenefit - Math.Max(0, grossEarnings - EarningsDisregard));

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
