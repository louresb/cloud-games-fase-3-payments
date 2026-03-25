using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SQS;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Application.Payments.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Fiap.CloudGames.Payments.Lambda;

public class Function
{
    private readonly ServiceProvider _serviceProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Function()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Fiap.CloudGames.Payments.Lambda")
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddSingleton(Log.Logger);
        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
        services.AddSingleton<IEventPublisher, SqsEventPublisher>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        using var scope = _serviceProvider.CreateScope();

        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var logger = scope.ServiceProvider.GetRequiredService<Serilog.ILogger>();

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = "Request body is required."
            };
        }

        var paymentEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(request.Body, JsonOptions);
        if (paymentEvent is null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = "Invalid request payload."
            };
        }

        logger.Information(
            "Publishing payment succeeded event to SQS. OrderId={OrderId}, TransactionId={PaymentTransactionId}",
            paymentEvent.OrderId,
            paymentEvent.PaymentTransactionId);

        await publisher.PublishAsync(paymentEvent, CancellationToken.None);

        logger.Information("Payment event published to SQS successfully.");
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = "PaymentSucceededEvent published to SQS."
        };
    }
}
