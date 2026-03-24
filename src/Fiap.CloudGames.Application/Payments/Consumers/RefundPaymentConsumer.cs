using Fiap.CloudGames.Application.Payments.Commands;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Application.Payments.Services;
using Fiap.CloudGames.Domain.Payments.Contracts;
using Fiap.CloudGames.Domain.Payments.Enums;
using Fiap.CloudGames.Domain.Payments.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Consumers;

public class RefundPaymentConsumer(
    ILogger<RefundPaymentConsumer> logger, 
    IEventPublisher eventPublisher,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway) : IConsumer<RefundPaymentCommand>
{
    private readonly ILogger<RefundPaymentConsumer> _logger = logger;
    private readonly IEventPublisher _eventPublisher = eventPublisher;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IPaymentGateway _paymentGateway = paymentGateway;
    
    public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
    {
        var cmd = context.Message;

        _logger.LogInformation("Processando reembolso para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);

        try
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Nenhum pagamento encontrado para o pedido {OrderId}. Não é possível processar o reembolso.", cmd.OrderId);
                return;
            }

            if (payment.Status == PaymentStatus.Refunded)
            {
                _logger.LogWarning("Pagamento para o pedido {OrderId} já está reembolsado. Ignorando processamento duplicado.", cmd.OrderId);
                return;
            }

            var (Refunded, FailedReason) = await _paymentGateway.ProcessRefundAsync(
                paymentTransactionId: payment.PaymentTransactionId,
                reason: cmd.Reason,
                ct: context.CancellationToken
            );

            if (!Refunded)
            {
                await _eventPublisher.PublishAsync(new PaymentFailedEvent
                (
                    OrderId: payment.OrderId,
                    UserEmail: payment.UserEmail,
                    FailedReason: FailedReason!
                ), context.CancellationToken);

                _logger.LogError("Falha ao processar reembolso para o pedido {OrderId}: {FailedReason}", cmd.OrderId, FailedReason);

                return;
            }

            payment.MarkAsRefunded(cmd.Reason);
            await _paymentRepository.UpdateAsync(payment, context.CancellationToken);

            await _eventPublisher.PublishAsync(new PaymentRefundedEvent
            (
                OrderId: payment.OrderId,
                UserEmail: payment.UserEmail,
                RefundedAt: DateTime.UtcNow
            ), context.CancellationToken);

            _logger.LogInformation("Reembolso processado com sucesso para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);
        }
        catch (Exception)
        {
            _logger.LogError("Erro ao processar reembolso para o pedido {OrderId} do usuário {UserId}.", cmd.OrderId, cmd.UserId);
            throw;
        }
    }
}
