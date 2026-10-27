using System.ComponentModel.DataAnnotations;

namespace ClaimsPortal.Core.Claims;

public enum BenefitProgram
{
    [Display(Name = "Unemployment Insurance")]
    UnemploymentInsurance,
    [Display(Name = "Disaster Assistance")]
    DisasterAssistance,
    [Display(Name = "Trade Adjustment")]
    TradeAdjustment,
    [Display(Name = "Work Share")]
    WorkShare,
}

public enum ClaimStatus
{
    [Display(Name = "Pending review")]
    PendingReview,
    [Display(Name = "Needs information")]
    NeedsInformation,
    Approved,
    Denied,
}

/// <summary>A weekly benefit claim filed by a claimant.</summary>
public class Claim
{
    public int Id { get; set; }

    /// <summary>Public reference shown to the claimant, for example CL-024051.</summary>
    public required string ClaimNumber { get; set; }

    public required string ClaimantName { get; set; }

    /// <summary>8 digit identifier from the claimant's approval letter.</summary>
    public required string ClaimantId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public BenefitProgram Program { get; set; }

    public DateOnly WeekEnding { get; set; }

    public bool WorkedThisWeek { get; set; }

    public decimal? GrossEarnings { get; set; }

    public bool AbleAndAvailable { get; set; }

    public decimal WeeklyAmount { get; set; }

    public ClaimStatus Status { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
