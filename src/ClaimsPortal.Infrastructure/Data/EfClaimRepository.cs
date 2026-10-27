using ClaimsPortal.Core.Claims;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPortal.Infrastructure.Data;

public sealed class EfClaimRepository(ClaimsDbContext db) : IClaimRepository
{
    public async Task<PagedResult<Claim>> QueryAsync(ClaimQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var claims = db.Claims.AsNoTracking();
        var total = await claims.CountAsync(cancellationToken);

        var items = await ApplySort(claims, query.Sort, query.Direction)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, query.Page, query.PageSize, total);
    }

    public Task<Claim?> GetByNumberAsync(string claimNumber, CancellationToken cancellationToken = default) =>
        db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimNumber == claimNumber, cancellationToken);

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        db.Claims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Claim number is always the tie breaker so paging is stable when sort values repeat
    private static IOrderedQueryable<Claim> ApplySort(IQueryable<Claim> source, ClaimSortField sort, SortDirection direction)
    {
        var ordered = (sort, direction) switch
        {
            (ClaimSortField.ClaimNumber, SortDirection.Ascending) => source.OrderBy(c => c.ClaimNumber),
            (ClaimSortField.ClaimNumber, _) => source.OrderByDescending(c => c.ClaimNumber),
            (ClaimSortField.Claimant, SortDirection.Ascending) => source.OrderBy(c => c.ClaimantName),
            (ClaimSortField.Claimant, _) => source.OrderByDescending(c => c.ClaimantName),
            (ClaimSortField.Program, SortDirection.Ascending) => source.OrderBy(c => c.Program),
            (ClaimSortField.Program, _) => source.OrderByDescending(c => c.Program),
            (ClaimSortField.WeekEnding, SortDirection.Ascending) => source.OrderBy(c => c.WeekEnding),
            (ClaimSortField.WeekEnding, _) => source.OrderByDescending(c => c.WeekEnding),
            (ClaimSortField.Status, SortDirection.Ascending) => source.OrderBy(c => c.Status),
            (ClaimSortField.Status, _) => source.OrderByDescending(c => c.Status),
            (ClaimSortField.Amount, SortDirection.Ascending) => source.OrderBy(c => c.WeeklyAmount),
            (ClaimSortField.Amount, _) => source.OrderByDescending(c => c.WeeklyAmount),
            (_, SortDirection.Ascending) => source.OrderBy(c => c.SubmittedAt),
            _ => source.OrderByDescending(c => c.SubmittedAt),
        };
        return ordered.ThenBy(c => c.ClaimNumber);
    }
}
