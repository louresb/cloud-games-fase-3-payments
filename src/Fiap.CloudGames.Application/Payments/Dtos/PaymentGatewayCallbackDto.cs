using System;

namespace Fiap.CloudGames.Application.Payments.Dtos;

/// <summary>
/// DTO para callback do gateway de pagamento.
/// </summary>
/// <param name="PaymentTransactionId">Id da Transação de Pagamento</param>
/// <param name="Status">Resultado da Transação</param>
public record PaymentGatewayCallbackDto(
    string PaymentTransactionId,
    string Status
);
