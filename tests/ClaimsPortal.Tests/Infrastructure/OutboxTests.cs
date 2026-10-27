using ClaimsPortal.Core.Claims;
using ClaimsPortal.Infrastructure.Data;
using ClaimsPortal.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClaimsPortal.Tests.Infrastructure;

public sealed class OutboxTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly OutboxDispatcher _dispatcher;

    public OutboxTests()
    {
        var services = new ServiceCollection()
            .AddScoped(_ => _database.CreateContext())
            .AddScoped(_ => _publisher.Object)
            .BuildServiceProvider();
        _dispatcher = new OutboxDispatcher(
            services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System, NullLogger<OutboxDispatcher>.Instance);
    }

    public void Dispose() => _database.Dispose();

    private async Task AddClaimAsync(string number)
    {
        await using var db = _database.CreateContext();
        await new EfClaimRepository(db).AddAsync(new Claim
        {
            ClaimNumber = number,
            ClaimantName = "Test Claimant",
            ClaimantId = "12345678",
            WeekEnding = new DateOnly(2026, 10, 24),
            WeeklyAmount = 370,
        });
    }

    [Fact]
    public async Task Saving_a_claim_writes_an_outbox_message_in_the_same_transaction()
    {
        await AddClaimAsync("CL-900001");

        await using var db = _database.CreateContext();
        var message = await db.OutboxMessages.SingleAsync();
        message.Type.Should().Be("claims.claim-submitted.v1");
        message.Payload.Should().Contain("\"ClaimNumber\":\"CL-900001\"");
        message.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task Dispatcher_publishes_pending_messages_and_marks_them_processed()
    {
        await AddClaimAsync("CL-900002");

        var sent = await _dispatcher.DispatchPendingAsync(CancellationToken.None);

        sent.Should().Be(1);
        _publisher.Verify(p => p.PublishAsync("claims.claim-submitted.v1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var db = _database.CreateContext();
        (await db.OutboxMessages.SingleAsync()).ProcessedAt.Should().NotBeNull();

        // Nothing left to send on the next pass
        (await _dispatcher.DispatchPendingAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task Failed_publish_is_recorded_and_retried_until_the_attempt_limit()
    {
        await AddClaimAsync("CL-900003");
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        for (var i = 0; i < OutboxDispatcher.MaxAttempts + 2; i++)
        {
            await _dispatcher.DispatchPendingAsync(CancellationToken.None);
        }

        await using var db = _database.CreateContext();
        var message = await db.OutboxMessages.SingleAsync();
        message.ProcessedAt.Should().BeNull();
        message.Attempts.Should().Be(OutboxDispatcher.MaxAttempts);
        message.LastError.Should().Be("Broker unavailable");
        _publisher.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(OutboxDispatcher.MaxAttempts));
    }

    [Fact]
    public async Task Repository_sorts_and_pages_with_a_stable_tie_breaker()
    {
        await using var db = _database.CreateContext();
        var repository = new EfClaimRepository(db);

        var first = await repository.QueryAsync(new ClaimQuery { Sort = ClaimSortField.Claimant, Direction = SortDirection.Ascending, PageSize = 5 });
        var last = await repository.QueryAsync(new ClaimQuery { Sort = ClaimSortField.Claimant, Direction = SortDirection.Ascending, PageSize = 5, Page = 5 });

        first.TotalCount.Should().Be(24);
        first.TotalPages.Should().Be(5);
        first.Items.Select(c => c.ClaimantName).Should().BeInAscendingOrder(StringComparer.Ordinal);
        first.Items[0].ClaimantName.Should().Be("Avery Johnson");
        last.Items.Should().HaveCount(4);
        last.FirstItem.Should().Be(21);
    }
}
