using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using ClaimsPortal.Core.Claims;

namespace ClaimsPortal.Web.Forms;

/// <summary>Formatting helpers shared by the views.</summary>
public static class DisplayText
{
    private static readonly ConcurrentDictionary<Enum, string> Names = new();
    private static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

    /// <summary>The [Display(Name)] of an enum value, or its name when there is none.</summary>
    public static string For(Enum value) => Names.GetOrAdd(value, v =>
        v.GetType().GetField(v.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? v.ToString());

    public static string Money(decimal amount) => amount.ToString("C0", UnitedStates);

    public static string Date(DateOnly date) => date.ToString("MMM d, yyyy", UnitedStates);

    /// <summary>CSS modifier for the status badge. The badge text always carries the status too.</summary>
    public static string BadgeClass(ClaimStatus status) => status switch
    {
        ClaimStatus.Approved => "badge--approved",
        ClaimStatus.NeedsInformation => "badge--needs",
        ClaimStatus.Denied => "badge--denied",
        _ => "badge--pending",
    };
}
