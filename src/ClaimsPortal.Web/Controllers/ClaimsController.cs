using ClaimsPortal.Core.Claims;
using ClaimsPortal.Web.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Web.Controllers;

public sealed class ClaimsController(ClaimService claims) : Controller
{
    [HttpGet("/claims")]
    public async Task<IActionResult> Index([FromQuery] ClaimQuery query, CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        var page = await claims.QueryAsync(normalized, cancellationToken);
        return View(new ClaimListViewModel(page, normalized));
    }

    [HttpGet("/claims/new")]
    public IActionResult New() => View(new ClaimForm());

    [HttpPost("/claims/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(ClaimForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        // Model binding errors are not used; everything is validated below with friendlier messages
        ModelState.Clear();

        var (request, formatErrors) = form.ToRequest();
        var errors = formatErrors.ToList();
        var result = await claims.SubmitAsync(request, cancellationToken);
        errors.AddRange(result.Errors.Where(e => errors.TrueForAll(f => f.Field != e.Field)));

        if (errors.Count > 0 || !result.Succeeded)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
            return View(form);
        }

        // Post, redirect, get: refreshing the confirmation page cannot submit the claim twice
        return RedirectToAction(nameof(Submitted), new { claimNumber = result.Claim!.ClaimNumber });
    }

    [HttpGet("/claims/{claimNumber}/submitted")]
    public async Task<IActionResult> Submitted(string claimNumber, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(claimNumber, cancellationToken);
        return claim is null ? NotFound() : View(claim);
    }
}

public sealed record ClaimListViewModel(PagedResult<Claim> Page, ClaimQuery Query)
{
    public static readonly IReadOnlyList<(ClaimSortField Field, string Label)> Columns =
    [
        (ClaimSortField.ClaimNumber, "Claim number"),
        (ClaimSortField.Claimant, "Claimant"),
        (ClaimSortField.Program, "Program"),
        (ClaimSortField.WeekEnding, "Week ending"),
        (ClaimSortField.Status, "Status"),
        (ClaimSortField.Amount, "Weekly amount"),
    ];

    /// <summary>Value for the th aria-sort attribute, or null when the column is not sorted.</summary>
    public string? AriaSort(ClaimSortField field) => Query.Sort != field
        ? null
        : Query.Direction == SortDirection.Ascending ? "ascending" : "descending";

    /// <summary>Clicking a sorted column flips its direction. A new column starts ascending.</summary>
    public SortDirection NextDirection(ClaimSortField field) =>
        Query.Sort == field && Query.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;

    public string SortedByText
    {
        get
        {
            var label = Columns.FirstOrDefault(c => c.Field == Query.Sort).Label ?? "date submitted";
            return $"sorted by {label.ToLowerInvariant()} {Query.Direction.ToString().ToLowerInvariant()}";
        }
    }
}
