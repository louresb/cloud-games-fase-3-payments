using System;

namespace Fiap.CloudGames.Domain.Payments.Contracts;

public interface IPaymentGateway
{
    Task<(string PaymentLinkUrl, string PaymentTransactionId)> GeneratePaymentLinkAsync(Guid orderId, decimal amount, CancellationToken ct);
    Task<(bool Refunded, string? FailedReason)> ProcessRefundAsync(string paymentTransactionId, string reason, CancellationToken ct);
}
