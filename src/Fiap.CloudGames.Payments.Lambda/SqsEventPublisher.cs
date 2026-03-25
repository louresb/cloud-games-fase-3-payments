using Amazon.SQS;
using Amazon.SQS.Model;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Application.Payments.Services;
using Serilog;

namespace Fiap.CloudGames.Payments.Lambda;

internal sealed class SqsEventPublisher(IAmazonSQS sqsClient, ILogger logger) : IEventPublisher
{
    private readonly IAmazonSQS _sqsClient = sqsClient;
    private readonly ILogger _logger = logger;

    public async Task PublishAsync<T>(T @event, CancellationToken ct) where T : class
    {
        var queueUrl = Environment.GetEnvironmentVariable("PAYMENTS_NOTIFICATIONS_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException("Environment variable PAYMENTS_NOTIFICATIONS_QUEUE_URL is required.");
        }

        var correlationId = Environment.GetEnvironmentVariable("CORRELATION_ID");
        var envelopeJson = EventEnvelopeMapper.ToEnvelopeJson(@event, correlationId);

        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["eventType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = typeof(T).Name
            }
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            messageAttributes["correlationId"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = correlationId
            };
        }

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = envelopeJson,
            MessageAttributes = messageAttributes
        };

        await _sqsClient.SendMessageAsync(request, ct);

        _logger.Information("Event envelope published to SQS. EventType={EventType}", typeof(T).Name);
    }
}
