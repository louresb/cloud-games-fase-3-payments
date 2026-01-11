using Fiap.CloudGames.Domain.Payments.Enums;

namespace Fiap.CloudGames.Domain.Payments.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }

    public string UserEmail { get; private set; } = string.Empty;

    public string PaymentLinkUrl { get; private set; } = string.Empty;
    public string PaymentTransactionId { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? StatusReason { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private Payment() { }

    private Payment(
        Guid id,
        Guid orderId,
        decimal amount,
        string userEmail,
        string paymentLinkUrl,
        string paymentTransactionId,
        PaymentStatus status,
        string? statusReason = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId obrigatório.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(userEmail)) throw new ArgumentException("UserEmail obrigatório.", nameof(userEmail));
        if (amount <= 0) throw new ArgumentException("Amount deve ser maior que zero.", nameof(amount));
        if (string.IsNullOrWhiteSpace(paymentLinkUrl)) throw new ArgumentException("PaymentLinkUrl obrigatório.", nameof(paymentLinkUrl));
        if (string.IsNullOrWhiteSpace(paymentTransactionId)) throw new ArgumentException("PaymentTransactionId obrigatório.", nameof(paymentTransactionId));

        Id = id;
        OrderId = orderId;
        Amount = amount;

        UserEmail = userEmail;

        PaymentLinkUrl = paymentLinkUrl.Trim();
        PaymentTransactionId = paymentTransactionId.Trim();

        Status = status;
        StatusReason = statusReason;
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }

    public static Payment Create(
        Guid orderId,
        decimal amount,
        string userEmail,
        string paymentTransactionId,
        string paymentLinkUrl,
        PaymentStatus status,
        string? statusReason = null) 
        => new(
            id: Guid.NewGuid(),
            orderId: orderId,
            amount: amount,
            userEmail: userEmail,
            paymentTransactionId: paymentTransactionId,
            paymentLinkUrl: paymentLinkUrl,
            status: status,
            statusReason: statusReason
        );
    
    public void MarkAsSucceeded()
    {
        if (Status == PaymentStatus.Succeeded)
            throw new InvalidOperationException("Pagamento já está marcado como pago.");
        Status = PaymentStatus.Succeeded;
        StatusReason = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsRefunded(string reason)
    {
        switch (Status)
        {
            case PaymentStatus.Refunded:
                throw new InvalidOperationException("Pagamento já está reembolsado.");
            case PaymentStatus.Cancelled:
                throw new InvalidOperationException("Pagamento está cancelado e não pode ser reembolsado.");
            case PaymentStatus.Failed:
                throw new InvalidOperationException("Pagamento falhou e não pode ser reembolsado.");
        }
        Status = PaymentStatus.Refunded;
        StatusReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        switch (Status)
        {
            case PaymentStatus.Refunded:
                throw new InvalidOperationException("Pagamento já está reembolsado.");
            case PaymentStatus.Cancelled:
                throw new InvalidOperationException("Pagamento está cancelado e não pode ser reembolsado.");
            case PaymentStatus.Failed:
                throw new InvalidOperationException("Pagamento falhou e não pode ser reembolsado.");
        }
        Status = PaymentStatus.Failed;
        StatusReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled(string reason)
    {
        switch (Status)
        {
            case PaymentStatus.Refunded:
                throw new InvalidOperationException("Pagamento já está reembolsado.");
            case PaymentStatus.Cancelled:
                throw new InvalidOperationException("Pagamento está cancelado e não pode ser reembolsado.");
            case PaymentStatus.Failed:
                throw new InvalidOperationException("Pagamento falhou e não pode ser reembolsado.");
        }
        Status = PaymentStatus.Cancelled;
        StatusReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
