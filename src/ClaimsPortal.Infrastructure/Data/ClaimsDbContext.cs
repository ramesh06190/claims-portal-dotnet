using System.Text.Json;
using ClaimsPortal.Core.Claims;
using ClaimsPortal.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClaimsPortal.Infrastructure.Data;

public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options, TimeProvider timeProvider) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Claim>(claim =>
        {
            claim.HasIndex(c => c.ClaimNumber).IsUnique();
            claim.Property(c => c.ClaimNumber).HasMaxLength(12);
            claim.Property(c => c.ClaimantName).HasMaxLength(ClaimValidator.MaxNameLength);
            claim.Property(c => c.ClaimantId).HasMaxLength(8).IsFixedLength();
            claim.Property(c => c.Email).HasMaxLength(254);
            claim.Property(c => c.Phone).HasMaxLength(30);
            claim.Property(c => c.Program).HasConversion<string>().HasMaxLength(30);
            claim.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
            claim.Property(c => c.GrossEarnings).HasPrecision(10, 2);
            claim.Property(c => c.WeeklyAmount).HasPrecision(10, 2);
            claim.HasData(SeedData.Claims());
        });

        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.Property(m => m.Type).HasMaxLength(100);
            // Lets the dispatcher find unsent messages without scanning the table
            outbox.HasIndex(m => new { m.ProcessedAt, m.OccurredAt });
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // The integration tests run on in-memory SQLite, which cannot sort or compare
        // DateTimeOffset and decimal columns. Store them as numbers there; SQL Server is unaffected.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
        }
    }

    /// <summary>
    /// Adds a ClaimSubmitted outbox message for every new claim, inside the same SaveChanges,
    /// so the claim and its event are committed together or not at all.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var newClaims = ChangeTracker.Entries<Claim>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var claim in newClaims)
        {
            var payload = new ClaimSubmittedEvent(claim.ClaimNumber, claim.ClaimantId, claim.WeekEnding, claim.WeeklyAmount);
            OutboxMessages.Add(new OutboxMessage
            {
                Type = "claims.claim-submitted.v1",
                Payload = JsonSerializer.Serialize(payload),
                OccurredAt = timeProvider.GetUtcNow(),
            });
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
