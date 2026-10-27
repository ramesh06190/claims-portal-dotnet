using ClaimsPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaimsPortal.Infrastructure.Messaging;

/// <summary>
/// Background worker that sends pending outbox messages. Failures are recorded and retried on the
/// next pass, and a message is parked after MaxAttempts so one bad message cannot block the rest.
/// </summary>
public sealed partial class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    public const int BatchSize = 20;
    public const int MaxAttempts = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogBatchFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Sends one batch. Public so it can be tested without the timer.</summary>
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Payload, message.Id.ToString(), cancellationToken);
                message.ProcessedAt = timeProvider.GetUtcNow();
                message.LastError = null;
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                LogMessageFailed(logger, ex, message.Id, message.Attempts);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return sent;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox batch failed")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} failed on attempt {Attempt}")]
    private static partial void LogMessageFailed(ILogger logger, Exception exception, Guid messageId, int attempt);
}
