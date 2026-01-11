using Fiap.CloudGames.Application.Payments.Dtos;

namespace Fiap.CloudGames.Application.Payments.Services;

public interface IPaymentService
{
    Task<string> ProcessTransactionAsync(PaymentGatewayCallbackDto dto, CancellationToken ct);
}
