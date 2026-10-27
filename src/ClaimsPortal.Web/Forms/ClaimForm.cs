using System.Globalization;
using ClaimsPortal.Core.Claims;

namespace ClaimsPortal.Web.Forms;

/// <summary>
/// What the HTML form posts. Everything is a string so that whatever someone typed can be
/// shown back to them when there is an error, instead of being lost by model binding.
/// </summary>
public sealed class ClaimForm
{
    public string? ClaimantName { get; set; }

    public string? ClaimantId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public BenefitProgram Program { get; set; } = BenefitProgram.UnemploymentInsurance;

    public string? WeekEndingDay { get; set; }

    public string? WeekEndingMonth { get; set; }

    public string? WeekEndingYear { get; set; }

    /// <summary>"yes", "no" or null when not answered.</summary>
    public string? WorkedThisWeek { get; set; }

    public string? GrossEarnings { get; set; }

    public string? AbleAndAvailable { get; set; }

    /// <summary>
    /// The order fields appear on the page, so the error summary lists problems in the same
    /// order, and the element each summary link should move focus to.
    /// </summary>
    public static readonly IReadOnlyList<(string Key, string FocusId)> FieldOrder =
    [
        (nameof(ClaimantName), nameof(ClaimantName)),
        (nameof(ClaimantId), nameof(ClaimantId)),
        (nameof(Email), nameof(Email)),
        ("WeekEnding", nameof(WeekEndingDay)),
        (nameof(WorkedThisWeek), "WorkedThisWeek-yes"),
        (nameof(GrossEarnings), nameof(GrossEarnings)),
        (nameof(AbleAndAvailable), "AbleAndAvailable-yes"),
    ];

    /// <summary>
    /// Converts the raw form into a request. Problems with the input format (a date that does not
    /// exist, earnings that are not a number) are returned as errors here, and the business rules
    /// are checked later by ClaimValidator.
    /// </summary>
    public (SubmitClaimRequest Request, List<ValidationError> FormatErrors) ToRequest()
    {
        var errors = new List<ValidationError>();
        var weekEnding = MemorableDate.Parse(WeekEndingDay, WeekEndingMonth, WeekEndingYear, "week ending date", out var dateError);
        if (dateError is not null)
        {
            errors.Add(new ValidationError("WeekEnding", dateError));
        }

        decimal? earnings = null;
        if (!string.IsNullOrWhiteSpace(GrossEarnings))
        {
            var cleaned = GrossEarnings.Trim().TrimStart('$').Replace(",", "", StringComparison.Ordinal);
            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                earnings = parsed;
            }
            else
            {
                errors.Add(new ValidationError(nameof(GrossEarnings), "Gross earnings must be a number, like 125.50"));
            }
        }

        var request = new SubmitClaimRequest
        {
            ClaimantName = ClaimantName,
            ClaimantId = ClaimantId,
            Email = Email,
            Phone = Phone,
            Program = Program,
            // A date that failed to parse is reported above, so it is left null without a second error
            WeekEnding = weekEnding,
            WorkedThisWeek = YesNo(WorkedThisWeek),
            GrossEarnings = earnings,
            AbleAndAvailable = YesNo(AbleAndAvailable),
        };
        return (request, errors);
    }

    private static bool? YesNo(string? value) => value switch
    {
        "yes" => true,
        "no" => false,
        _ => null,
    };
}

/// <summary>
/// Parses the three part "memorable date" input (day, month, year) with error messages that
/// name the missing part, matching the client side component on the portfolio site.
/// </summary>
public static class MemorableDate
{
    public static DateOnly? Parse(string? day, string? month, string? year, string label, out string? error)
    {
        error = null;
        day = day?.Trim();
        month = month?.Trim();
        year = year?.Trim();
        var parts = new[] { ("day", day), ("month", month), ("year", year) };

        if (parts.All(p => string.IsNullOrEmpty(p.Item2)))
        {
            // Empty is not a format problem; the business rule reports "Enter the ..." instead
            return null;
        }

        var missing = parts.Where(p => string.IsNullOrEmpty(p.Item2)).Select(p => p.Item1).ToList();
        if (missing.Count > 0)
        {
            error = $"The {label} must include a {string.Join(" and ", missing)}";
            return null;
        }

        if (year!.Length == 4
            && int.TryParse(day, NumberStyles.None, CultureInfo.InvariantCulture, out var d)
            && int.TryParse(month, NumberStyles.None, CultureInfo.InvariantCulture, out var m)
            && int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out var y)
            && m is >= 1 and <= 12
            && y >= 1900
            && d >= 1 && d <= DateTime.DaysInMonth(y, m))
        {
            return new DateOnly(y, m, d);
        }

        error = $"The {label} must be a real date";
        return null;
    }
}
