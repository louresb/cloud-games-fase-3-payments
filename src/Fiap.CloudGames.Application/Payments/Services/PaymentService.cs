using Fiap.CloudGames.Application.Payments.Dtos;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Payments.Repositories;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Services;

public class PaymentService(
    ILogger<PaymentService> logger,
    IPaymentRepository repository,
    IEventPublisher eventPublisher) : IPaymentService
{
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly IPaymentRepository _repository = repository;
    private readonly IEventPublisher _eventPublisher = eventPublisher;

    public async Task<string> ProcessTransactionAsync(PaymentGatewayCallbackDto dto, CancellationToken ct)
    {
        var payment = await _repository.GetByPaymentTransactionId(dto.PaymentTransactionId, ct);
    
        if (payment == null)
            throw new ArgumentException("Pagamento não encontrado para a transação fornecida.", nameof(dto.PaymentTransactionId));

        switch (dto.Status.ToLower())
        {
            case "success":
                _logger.LogInformation("Recebido callback de pagamento sucedido para PaymentTransactionId: {PaymentTransactionId}", dto.PaymentTransactionId);
                payment.MarkAsSucceeded();
                await _repository.UpdateAsync(payment, ct);

                await _eventPublisher.PublishAsync(new PaymentSucceededEvent
                (
                    OrderId: payment.OrderId,
                    UserEmail: payment.UserEmail,
                    PaymentTransactionId: payment.PaymentTransactionId,
                    ProcessedAt: DateTime.UtcNow
                ), ct);
                
                return "Pagamento marcado como sucedido com sucesso.";
            case "cancelled":
                _logger.LogInformation("Recebido callback de pagamento cancelado para PaymentTransactionId: {PaymentTransactionId}", dto.PaymentTransactionId);
                payment.MarkAsCancelled("Cancelado via callback do gateway");
                await _repository.UpdateAsync(payment, ct);

                await _eventPublisher.PublishAsync(new PaymentFailedEvent
                (
                    OrderId: payment.OrderId,
                    UserEmail: payment.UserEmail,
                    FailedReason: "Pagamento cancelado no gateway."
                ), ct);
                
                return "Pagamento cancelado com sucesso.";
            case "failed":
                _logger.LogInformation("Recebido callback de pagamento falho para PaymentTransactionId: {PaymentTransactionId}", dto.PaymentTransactionId);
                payment.MarkAsFailed("Falha via callback do gateway");
                await _repository.UpdateAsync(payment, ct);

                await _eventPublisher.PublishAsync(new PaymentFailedEvent
                (
                    OrderId: payment.OrderId,
                    UserEmail: payment.UserEmail,
                    FailedReason: "Pagamento falhou no gateway."
                ), ct);
                
                return "Pagamento marcado como falho com sucesso.";
            default:
                _logger.LogWarning("Status desconhecido recebido no callback do gateway: {Status}", dto.Status);
                throw new ArgumentException("Status desconhecido recebido do gateway de pagamento.", nameof(dto.Status));
        }
    }
}
