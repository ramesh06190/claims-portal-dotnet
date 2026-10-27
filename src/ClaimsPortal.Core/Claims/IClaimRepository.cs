namespace ClaimsPortal.Core.Claims;

public interface IClaimRepository
{
    Task<PagedResult<Claim>> QueryAsync(ClaimQuery query, CancellationToken cancellationToken = default);

    Task<Claim?> GetByNumberAsync(string claimNumber, CancellationToken cancellationToken = default);

    /// <summary>Saves the claim. Implementations also record a ClaimSubmitted integration event.</summary>
    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);
}

public interface IClaimNumberGenerator
{
    string NewNumber();
}

public sealed class RandomClaimNumberGenerator : IClaimNumberGenerator
{
    public string NewNumber() => $"CL-{Random.Shared.Next(100_000, 1_000_000):D6}";
}
