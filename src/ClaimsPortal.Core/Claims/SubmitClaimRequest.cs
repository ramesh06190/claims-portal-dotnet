namespace ClaimsPortal.Core.Claims;

/// <summary>
/// Input for filing a weekly claim. Fields are nullable so that "not answered" can be told
/// apart from a real answer and reported with a specific error message.
/// </summary>
public sealed record SubmitClaimRequest
{
    public string? ClaimantName { get; init; }

    public string? ClaimantId { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public BenefitProgram Program { get; init; } = BenefitProgram.UnemploymentInsurance;

    public DateOnly? WeekEnding { get; init; }

    public bool? WorkedThisWeek { get; init; }

    public decimal? GrossEarnings { get; init; }

    public bool? AbleAndAvailable { get; init; }
}

/// <summary>One problem with one field. Field matches the request property name.</summary>
public sealed record ValidationError(string Field, string Message);
