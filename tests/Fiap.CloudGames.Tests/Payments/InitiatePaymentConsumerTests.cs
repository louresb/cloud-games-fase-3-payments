using Fiap.CloudGames.Application.Payments.Commands;
using Fiap.CloudGames.Application.Payments.Consumers;
using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Payments.Contracts;
using Fiap.CloudGames.Domain.Payments.Entities;
using Fiap.CloudGames.Domain.Payments.Enums;
using Fiap.CloudGames.Domain.Payments.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fiap.CloudGames.Tests.Payments;

public class InitiatePaymentConsumerTests
{
    [Fact]
    public async Task Consume_WhenNoExistingPayment_CreatesAndPublishesLink()
    {
        var logger = Mock.Of<ILogger<InitiatePaymentConsumer>>();
        var publish = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var gateway = new Mock<IPaymentGateway>(MockBehavior.Strict);

        var cmd = new InitiatePaymentCommand(Guid.NewGuid(), 99.9m, Guid.NewGuid(), "user@cloudgames.dev");

        repo.Setup(r => r.GetByOrderIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        gateway.Setup(g => g.GeneratePaymentLinkAsync(cmd.OrderId, cmd.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://pay/link", "txn-abc"));

        repo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        publish.Setup(p => p.Publish(It.IsAny<PaymentLinkGeneratedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new InitiatePaymentConsumer(logger, publish.Object, repo.Object, gateway.Object);
        var ctx = Mock.Of<ConsumeContext<InitiatePaymentCommand>>(c => c.Message == cmd && c.CancellationToken == CancellationToken.None);

        await consumer.Consume(ctx);

        repo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        publish.Verify(p => p.Publish(It.IsAny<PaymentLinkGeneratedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_WhenExistingPayment_IgnoresDuplicate()
    {
        var logger = Mock.Of<ILogger<InitiatePaymentConsumer>>();
        var publish = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var repo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var gateway = new Mock<IPaymentGateway>(MockBehavior.Strict);

        var cmd = new InitiatePaymentCommand(Guid.NewGuid(), 50m, Guid.NewGuid(), "user@cloudgames.dev");
        var existing = Payment.Create(cmd.OrderId, cmd.Amount, cmd.UserEmail, "txn", "http://link", PaymentStatus.Pending);

        repo.Setup(r => r.GetByOrderIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var consumer = new InitiatePaymentConsumer(logger, publish.Object, repo.Object, gateway.Object);
        var ctx = Mock.Of<ConsumeContext<InitiatePaymentCommand>>(c => c.Message == cmd && c.CancellationToken == CancellationToken.None);

        await consumer.Consume(ctx);

        publish.VerifyNoOtherCalls();
        gateway.VerifyNoOtherCalls();
        repo.Verify(r => r.GetByOrderIdAsync(cmd.OrderId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
