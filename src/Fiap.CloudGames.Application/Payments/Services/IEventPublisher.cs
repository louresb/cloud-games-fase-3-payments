namespace Fiap.CloudGames.Application.Payments.Services;

/// <summary>
/// Abstraction for publishing domain events.
/// Enables switching between RabbitMQ, EventBridge, or other brokers
/// without changing business logic or event contracts.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the configured event bus.
    /// </summary>
    /// <typeparam name="T">The type of event to publish</typeparam>
    /// <param name="event">The event instance to publish</param>
    /// <param name="ct">Cancellation token</param>
    Task PublishAsync<T>(T @event, CancellationToken ct) where T : class;
}
