using Microsoft.Extensions.Logging;

namespace ClaimsPortal.Infrastructure.Messaging;

/// <summary>Sends an integration event to a message broker.</summary>
public interface IMessagePublisher
{
    Task PublishAsync(string type, string payload, string messageId, CancellationToken cancellationToken);
}

/// <summary>
/// Stand-in publisher that logs the message. In production this would be an Azure Service Bus
/// sender: create a ServiceBusMessage with MessageId set to messageId (so duplicate detection
/// can drop retries) and Subject set to type, then call SendMessageAsync.
/// </summary>
public sealed partial class LoggingMessagePublisher(ILogger<LoggingMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync(string type, string payload, string messageId, CancellationToken cancellationToken)
    {
        LogPublished(logger, type, messageId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Type} message {MessageId}")]
    private static partial void LogPublished(ILogger logger, string type, string messageId);
}
