using ClaimsPortal.Core.Claims;

namespace ClaimsPortal.Infrastructure.Data;

/// <summary>Made up sample claims so the list has something to sort and page through.</summary>
internal static class SeedData
{
    private static readonly string[] Names =
    [
        "Avery Johnson", "Jordan Smith", "Taylor Nguyen", "Morgan Patel", "Casey Martinez", "Riley Thompson",
        "Jamie Robinson", "Quinn Walker", "Drew Hernandez", "Skyler Lee", "Reese Clark", "Rowan Lewis",
        "Parker Young", "Hayden King", "Emerson Wright", "Finley Scott", "Sawyer Green", "Dakota Adams",
        "Elliot Baker", "Harper Hill", "Kendall Rivera", "Logan Campbell", "Peyton Mitchell", "Remy Carter",
    ];

    public static IEnumerable<Claim> Claims()
    {
        var programs = Enum.GetValues<BenefitProgram>();
        var statuses = Enum.GetValues<ClaimStatus>();
        // A fixed Saturday so the seed is identical on every run (required by EF Core HasData)
        var lastSaturday = new DateOnly(2026, 10, 17);

        for (var i = 0; i < Names.Length; i++)
        {
            var weekEnding = lastSaturday.AddDays(-7 * (i % 10));
            var worked = i % 3 == 0;
            decimal? earnings = worked ? 40 + (i * 23 % 300) : null;

            yield return new Claim
            {
                Id = i + 1,
                ClaimNumber = $"CL-{24051 + (i * 37):D6}",
                ClaimantName = Names[i],
                ClaimantId = $"{40000000 + (i * 1913):D8}",
                Program = programs[i * 3 % programs.Length],
                WeekEnding = weekEnding,
                WorkedThisWeek = worked,
                GrossEarnings = earnings,
                AbleAndAvailable = i % 7 != 0,
                WeeklyAmount = ClaimService.CalculateWeeklyAmount(earnings ?? 0),
                Status = statuses[i * 5 % statuses.Length],
                SubmittedAt = new DateTimeOffset(weekEnding.ToDateTime(new TimeOnly(9, i % 60)), TimeSpan.Zero).AddDays(1),
            };
        }
    }
}
