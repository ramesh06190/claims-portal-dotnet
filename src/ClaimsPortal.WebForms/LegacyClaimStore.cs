using System;
using System.Collections.Generic;
using System.Linq;

namespace ClaimsPortal.WebForms
{
    public sealed class LegacyClaim
    {
        public string ClaimNumber { get; set; } = "";
        public string ClaimantId { get; set; } = "";
        public DateTime WeekEnding { get; set; }
        public string Status { get; set; } = "";
        public decimal WeeklyAmount { get; set; }
    }

    /// <summary>In memory sample data, standing in for the database a legacy app would call.</summary>
    public static class LegacyClaimStore
    {
        public static readonly string[] Statuses = { "Pending review", "Needs information", "Approved", "Denied" };

        private static readonly List<LegacyClaim> Claims = Enumerable.Range(0, 12).Select(i => new LegacyClaim
        {
            ClaimNumber = "CL-" + (24051 + (i * 37)).ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
            ClaimantId = i < 6 ? "12345678" : "87654321",
            WeekEnding = new DateTime(2026, 10, 17).AddDays(-7 * (i % 6)),
            Status = Statuses[i * 5 % Statuses.Length],
            WeeklyAmount = i % 3 == 0 ? 311m : 370m,
        }).ToList();

        public static IList<LegacyClaim> Find(string claimantId, string? status) =>
            Claims
                .Where(c => c.ClaimantId == claimantId && (string.IsNullOrEmpty(status) || c.Status == status))
                .OrderByDescending(c => c.WeekEnding)
                .ToList();
    }
}
