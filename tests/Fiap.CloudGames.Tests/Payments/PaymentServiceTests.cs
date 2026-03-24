using Fiap.CloudGames.Application.Payments.Dtos;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Application.Payments.Services;
using Fiap.CloudGames.Domain.Payments.Entities;
using Fiap.CloudGames.Domain.Payments.Enums;
using Fiap.CloudGames.Domain.Payments.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fiap.CloudGames.Tests.Payments;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessTransaction_Success_UpdatesAndPublishes()
    {
        var logger = Mock.Of<ILogger<PaymentService>>();
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);

        var payment = Payment.Create(Guid.NewGuid(), 10m, "u@e.com", "txn-1", "http://l", PaymentStatus.Pending);
        var dto = new PaymentGatewayCallbackDto(payment.PaymentTransactionId, "success");

        repo.Setup(r => r.GetByPaymentTransactionId(payment.PaymentTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        repo.Setup(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.PublishAsync(It.IsAny<PaymentSucceededEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new PaymentService(logger, repo.Object, publisher.Object);
        var msg = await svc.ProcessTransactionAsync(dto, CancellationToken.None);

        Assert.Contains("sucedido", msg);
        repo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublishAsync(It.IsAny<PaymentSucceededEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("failed")]
    public async Task ProcessTransaction_FailurePaths_UpdatesAndPublishesFailed(string status)
    {
        var logger = Mock.Of<ILogger<PaymentService>>();
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);

        var payment = Payment.Create(Guid.NewGuid(), 10m, "u@e.com", "txn-2", "http://l", PaymentStatus.Pending);
        var dto = new PaymentGatewayCallbackDto(payment.PaymentTransactionId, status);

        repo.Setup(r => r.GetByPaymentTransactionId(payment.PaymentTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        repo.Setup(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.PublishAsync(It.IsAny<PaymentFailedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new PaymentService(logger, repo.Object, publisher.Object);
        var msg = await svc.ProcessTransactionAsync(dto, CancellationToken.None);

        Assert.True(msg.Contains("cancelado") || msg.Contains("falho"));
        repo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublishAsync(It.IsAny<PaymentFailedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_UnknownStatus_Throws()
    {
        var logger = Mock.Of<ILogger<PaymentService>>();
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);

        var payment = Payment.Create(Guid.NewGuid(), 10m, "u@e.com", "txn-3", "http://l", PaymentStatus.Pending);
        var dto = new PaymentGatewayCallbackDto(payment.PaymentTransactionId, "weird");

        repo.Setup(r => r.GetByPaymentTransactionId(payment.PaymentTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var svc = new PaymentService(logger, repo.Object, publisher.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => svc.ProcessTransactionAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessTransaction_NotFound_Throws()
    {
        var logger = Mock.Of<ILogger<PaymentService>>();
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);

        var dto = new PaymentGatewayCallbackDto("missing", "success");
        repo.Setup(r => r.GetByPaymentTransactionId(dto.PaymentTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var svc = new PaymentService(logger, repo.Object, publisher.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => svc.ProcessTransactionAsync(dto, CancellationToken.None));
    }
}
