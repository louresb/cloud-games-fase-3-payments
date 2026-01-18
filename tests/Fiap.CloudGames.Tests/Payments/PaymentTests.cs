using Fiap.CloudGames.Domain.Payments.Entities;
using Fiap.CloudGames.Domain.Payments.Enums;

namespace Fiap.CloudGames.Tests.Payments;

public class PaymentTests
{
    [Fact]
    public void Create_Valid_SetsFieldsAndDefaults()
    {
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(
            orderId: orderId,
            amount: 150.50m,
            userEmail: "user@example.com",
            paymentTransactionId: "txn-123",
            paymentLinkUrl: "https://pay/link",
            status: PaymentStatus.Pending
        );

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(orderId, payment.OrderId);
        Assert.Equal(150.50m, payment.Amount);
        Assert.Equal("user@example.com", payment.UserEmail);
        Assert.Equal("https://pay/link", payment.PaymentLinkUrl);
        Assert.Equal("txn-123", payment.PaymentTransactionId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.StatusReason);
        Assert.NotEqual(default, payment.CreatedAt);
        Assert.Null(payment.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(
            orderId: Guid.NewGuid(),
            amount: amount,
            userEmail: "user@example.com",
            paymentTransactionId: "txn",
            paymentLinkUrl: "http://link",
            status: PaymentStatus.Pending
        ));
    }

    [Fact]
    public void Create_InvalidOrderId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(
            orderId: Guid.Empty,
            amount: 10,
            userEmail: "user@example.com",
            paymentTransactionId: "txn",
            paymentLinkUrl: "http://link",
            status: PaymentStatus.Pending
        ));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidStrings_Throw(string str)
    {
        var orderId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => Payment.Create(orderId, 10, str!, "txn", "http://link", PaymentStatus.Pending));
        Assert.Throws<ArgumentException>(() => Payment.Create(orderId, 10, "user@example.com", str!, "http://link", PaymentStatus.Pending));
        Assert.Throws<ArgumentException>(() => Payment.Create(orderId, 10, "user@example.com", "txn", str!, PaymentStatus.Pending));
    }

    [Fact]
    public void MarkAsSucceeded_TransitionsAndSetsUpdatedAt()
    {
        var p = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Pending);
        p.MarkAsSucceeded();
        Assert.Equal(PaymentStatus.Succeeded, p.Status);
        Assert.Null(p.StatusReason);
        Assert.NotNull(p.UpdatedAt);
    }

    [Fact]
    public void MarkAsSucceeded_Twice_Throws()
    {
        var p = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Pending);
        p.MarkAsSucceeded();
        Assert.Throws<InvalidOperationException>(() => p.MarkAsSucceeded());
    }

    [Fact]
    public void MarkAsRefunded_FromPending_SetsStatusAndReason()
    {
        var p = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Pending);
        p.MarkAsRefunded("cliente solicitou");
        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.Equal("cliente solicitou", p.StatusReason);
        Assert.NotNull(p.UpdatedAt);
    }

    [Fact]
    public void MarkAsRefunded_FromCancelledOrFailed_Throws()
    {
        var cancelled = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Cancelled);
        Assert.Throws<InvalidOperationException>(() => cancelled.MarkAsRefunded("r"));
        var failed = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Failed);
        Assert.Throws<InvalidOperationException>(() => failed.MarkAsRefunded("r"));
    }

    [Fact]
    public void MarkAsFailed_FromPending_SetsStatusAndReason()
    {
        var p = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Pending);
        p.MarkAsFailed("erro gateway");
        Assert.Equal(PaymentStatus.Failed, p.Status);
        Assert.Equal("erro gateway", p.StatusReason);
        Assert.NotNull(p.UpdatedAt);
    }

    [Fact]
    public void MarkAsCancelled_FromPending_SetsStatusAndReason()
    {
        var p = Payment.Create(Guid.NewGuid(), 10, "u@e.com", "t1", "http://l", PaymentStatus.Pending);
        p.MarkAsCancelled("cancelado");
        Assert.Equal(PaymentStatus.Cancelled, p.Status);
        Assert.Equal("cancelado", p.StatusReason);
        Assert.NotNull(p.UpdatedAt);
    }
}
