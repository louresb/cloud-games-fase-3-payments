namespace Fiap.CloudGames.Application.Payments.Events;

/// <summary>
/// Generic envelope for publishing events through different brokers.
/// Wraps domain events for compatibility with SQS/Lambda consumption.
/// </summary>
public record EventEnvelope
{
    /// <summary>
    /// Fully qualified type name of the domain event (e.g., "PaymentSucceededEvent")
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The domain event payload as JSON object
    /// </summary>
    public required object Payload { get; init; }

    /// <summary>
    /// Timestamp when event was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; init; }
}
