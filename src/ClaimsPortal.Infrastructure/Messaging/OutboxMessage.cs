namespace ClaimsPortal.Infrastructure.Messaging;

/// <summary>
/// An integration event waiting to be sent to other systems. It is written in the same
/// database transaction as the change that caused it, so an event is never lost and never
/// sent for a change that was rolled back (the transactional outbox pattern).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Type { get; set; }

    public required string Payload { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}

/// <summary>Payload of the event raised when a weekly claim is filed.</summary>
public sealed record ClaimSubmittedEvent(string ClaimNumber, string ClaimantId, DateOnly WeekEnding, decimal WeeklyAmount);
