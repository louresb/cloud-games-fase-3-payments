namespace Fiap.CloudGames.Application.Payments.Events;

public record PaymentLinkGeneratedEvent(
    Guid OrderId, 
    string UserEmail, 
    string PaymentTransactionId,
    string PaymentLinkUrl
);

public record PaymentSucceededEvent(
    Guid OrderId, 
    string UserEmail, 
    string PaymentTransactionId,
    DateTime ProcessedAt
);

public record PaymentFailedEvent(
    Guid OrderId, 
    string UserEmail, 
    string FailedReason
);

public record PaymentRefundFailedEvent(
    Guid OrderId, 
    string UserEmail, 
    string FailedReason
);

public record PaymentRefundedEvent(
    Guid OrderId, 
    string UserEmail, 
    DateTime RefundedAt
);
