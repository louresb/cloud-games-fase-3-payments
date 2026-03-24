namespace Fiap.CloudGames.Application.Payments.Events;

/// <summary>
/// Maps payment domain events to EventEnvelope format.
/// Used for serializing events to SQS/Lambda-compatible format.
/// </summary>
public static class EventEnvelopeMapper
{
    /// <summary>
    /// Converts a domain event to an EventEnvelope.
    /// </summary>
    public static EventEnvelope ToEnvelope<T>(T @event, string? correlationId = null) where T : class
    {
        return new EventEnvelope
        {
            EventType = typeof(T).Name,
            Payload = @event,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Converts a domain event to an EventEnvelope with JSON serialization.
    /// Ready for SQS transmission.
    /// </summary>
    public static string ToEnvelopeJson<T>(T @event, string? correlationId = null) where T : class
    {
        var envelope = ToEnvelope(@event, correlationId);
        return System.Text.Json.JsonSerializer.Serialize(envelope);
    }

    /// <summary>
    /// Maps all supported payment event types to their respective EventEnvelopes.
    /// Centralized mapping for reference and future extensibility.
    /// </summary>
    public static EventEnvelope MapPaymentEvent(object @event, string? correlationId = null)
    {
        return @event switch
        {
            PaymentSucceededEvent pse => ToEnvelope(pse, correlationId),
            PaymentFailedEvent pfe => ToEnvelope(pfe, correlationId),
            PaymentRefundedEvent pre => ToEnvelope(pre, correlationId),
            PaymentLinkGeneratedEvent plge => ToEnvelope(plge, correlationId),
            PaymentRefundFailedEvent prfe => ToEnvelope(prfe, correlationId),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}")
        };
    }
}
