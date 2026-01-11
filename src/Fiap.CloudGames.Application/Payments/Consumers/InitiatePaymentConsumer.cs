using Fiap.CloudGames.Application.Payments.Commands;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Payments.Contracts;
using Fiap.CloudGames.Domain.Payments.Entities;
using Fiap.CloudGames.Domain.Payments.Enums;
using Fiap.CloudGames.Domain.Payments.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Consumers;

public class InitiatePaymentConsumer(
    ILogger<InitiatePaymentConsumer> logger, 
    IPublishEndpoint publishEndpoint,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway) : IConsumer<InitiatePaymentCommand>
{
    private readonly ILogger<InitiatePaymentConsumer> _logger = logger;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IPaymentGateway _paymentGateway = paymentGateway;

    public async Task Consume(ConsumeContext<InitiatePaymentCommand> context)
    {
        var cmd = context.Message;

        _logger.LogInformation("Iniciando pagamento para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);

        try
        {
            var existingPayment = await _paymentRepository.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);
            if (existingPayment != null)
            {
                _logger.LogWarning("Pagamento já existe para o pedido {OrderId}. Ignorando processamento duplicado.", cmd.OrderId);
                return;
            }
            
            var (PaymentLinkUrl, PaymentTransactionId) = await _paymentGateway.GeneratePaymentLinkAsync(
                orderId: cmd.OrderId,
                amount: cmd.Amount,
                ct: context.CancellationToken
            );

            var payment = Payment.Create(
                orderId: cmd.OrderId,
                amount: cmd.Amount,
                userEmail: cmd.UserEmail,
                paymentTransactionId: PaymentTransactionId,
                paymentLinkUrl: PaymentLinkUrl,
                status: PaymentStatus.Pending
            );

            await _paymentRepository.AddAsync(payment, context.CancellationToken);

            await _publishEndpoint.Publish(new PaymentLinkGeneratedEvent
            (
                OrderId: payment.OrderId,
                UserEmail: payment.UserEmail,
                PaymentTransactionId: payment.PaymentTransactionId,
                PaymentLinkUrl: payment.PaymentLinkUrl
            ), context.CancellationToken);

            _logger.LogInformation("Pagamento iniciado com sucesso para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);
        }
        catch (Exception)
        {
            _logger.LogError("Erro ao iniciar pagamento para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);
            throw;
        }
    }
}
