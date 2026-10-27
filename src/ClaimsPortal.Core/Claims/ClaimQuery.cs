namespace ClaimsPortal.Core.Claims;

public enum ClaimSortField
{
    Submitted,
    ClaimNumber,
    Claimant,
    Program,
    WeekEnding,
    Status,
    Amount,
}

public enum SortDirection
{
    Ascending,
    Descending,
}

/// <summary>Paging and sorting options for the claims list. Out of range values are clamped.</summary>
public sealed record ClaimQuery
{
    public const int MaxPageSize = 50;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public ClaimSortField Sort { get; init; } = ClaimSortField.Submitted;

    public SortDirection Direction { get; init; } = SortDirection.Descending;

    public ClaimQuery Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
    };
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public int FirstItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItem => Math.Min(Page * PageSize, TotalCount);
}
