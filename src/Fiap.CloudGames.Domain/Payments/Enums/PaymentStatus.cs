namespace Fiap.CloudGames.Domain.Payments.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Refunded = 2,
    Cancelled = 3,
    Failed = 4
}
