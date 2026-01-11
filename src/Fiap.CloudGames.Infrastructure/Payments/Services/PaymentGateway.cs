using Fiap.CloudGames.Domain.Payments.Contracts;

namespace Fiap.CloudGames.Infrastructure.Payments.Services;

public class PaymentGateway : IPaymentGateway
{
    public async Task<(string PaymentLinkUrl, string PaymentTransactionId)> GeneratePaymentLinkAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        // Simula a chamada a um gateway de pagamento externo.
        await Task.Delay(500, ct); // Simula latência de rede.

        // Gera um link de pagamento fictício.
        var paymentTransactionId = Guid.NewGuid().ToString();
        var paymentLinkUrl = $"https://payment-gateway.com/pay/{paymentTransactionId}";

        return (paymentLinkUrl, paymentTransactionId);
    }

    public async Task<(bool Refunded, string? FailedReason)> ProcessRefundAsync(string paymentTransactionId, string reason, CancellationToken ct)
    {
        // Simula a chamada a um gateway de pagamento externo para processar o reembolso.
        await Task.Delay(500, ct); // Simula latência de rede.

        // Simula sucesso ou falha do reembolso.
        var success = !paymentTransactionId.Contains("fail", StringComparison.CurrentCultureIgnoreCase);

        if (success)
        {
            return (true, null);
        }
        else
        {
            return (false, "Falha ao processar o reembolso no gateway de pagamento.");
        }
    }
}
