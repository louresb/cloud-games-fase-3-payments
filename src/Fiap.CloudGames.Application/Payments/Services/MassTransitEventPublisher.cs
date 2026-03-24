using MassTransit;

namespace Fiap.CloudGames.Application.Payments.Services;

/// <summary>
/// MassTransit implementation of IEventPublisher.
/// Publishes events via MassTransit/RabbitMQ.
/// Will be replaced by EventBridgeEventPublisher in Phase B (AWS integration).
/// </summary>
public class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task PublishAsync<T>(T @event, CancellationToken ct) where T : class
    {
        await _publishEndpoint.Publish(@event, ct);
    }
}
