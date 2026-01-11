using System;
using Fiap.CloudGames.Domain.Payments.Entities;

namespace Fiap.CloudGames.Domain.Payments.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task<Payment?> GetByPaymentTransactionId(string paymentTransactionId, CancellationToken ct);

    Task<(IReadOnlyList<Payment> Items, int Total)> QueryAllAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<(IReadOnlyList<Payment> Items, int Total)> QueryForUserAsync(
        string userEmail,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task AddAsync(Payment payment, CancellationToken ct);
    Task UpdateAsync(Payment payment, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
