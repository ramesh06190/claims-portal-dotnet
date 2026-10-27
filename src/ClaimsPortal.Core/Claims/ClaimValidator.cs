using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ClaimsPortal.Core.Claims;

/// <summary>
/// Business rules for a weekly claim. Messages are written for the claimant, say how to fix
/// the problem, and match the wording used in the front-end error summary.
/// </summary>
public static partial class ClaimValidator
{
    public const int MaxNameLength = 100;

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex ClaimantIdPattern();

    public static IReadOnlyList<ValidationError> Validate(SubmitClaimRequest request, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();

        void Add(string field, string message) => errors.Add(new ValidationError(field, message));

        var name = request.ClaimantName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Add(nameof(request.ClaimantName), "Enter your full name");
        }
        else if (name.Length > MaxNameLength)
        {
            Add(nameof(request.ClaimantName), $"Full name must be {MaxNameLength} characters or fewer");
        }

        var claimantId = request.ClaimantId?.Trim();
        if (string.IsNullOrEmpty(claimantId))
        {
            Add(nameof(request.ClaimantId), "Enter your claimant ID");
        }
        else if (!ClaimantIdPattern().IsMatch(claimantId))
        {
            Add(nameof(request.ClaimantId), "Claimant ID must be 8 digits, like 12345678");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsEmail(request.Email.Trim()))
        {
            Add(nameof(request.Email), "Enter an email address in the correct format, like name@example.com");
        }

        if (request.WeekEnding is null)
        {
            Add(nameof(request.WeekEnding), "Enter the week ending date");
        }
        else if (request.WeekEnding.Value > today)
        {
            Add(nameof(request.WeekEnding), "The week ending date must be today or in the past");
        }
        else if (request.WeekEnding.Value.DayOfWeek != DayOfWeek.Saturday)
        {
            Add(nameof(request.WeekEnding), "The week ending date must be a Saturday");
        }

        if (request.WorkedThisWeek is null)
        {
            Add(nameof(request.WorkedThisWeek), "Select yes if you worked during this week");
        }
        else if (request.WorkedThisWeek == true && request.GrossEarnings is null)
        {
            Add(nameof(request.GrossEarnings), "Enter your gross earnings for this week");
        }

        if (request.GrossEarnings is < 0)
        {
            Add(nameof(request.GrossEarnings), "Gross earnings must be 0 or more");
        }

        if (request.AbleAndAvailable is null)
        {
            Add(nameof(request.AbleAndAvailable), "Select yes if you were able and available for work");
        }

        return errors;
    }

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value && value.Contains('.', StringComparison.Ordinal);
}
