using System;
using Fiap.CloudGames.Domain.Payments.Entities;
using Fiap.CloudGames.Domain.Payments.Enums;
using Fiap.CloudGames.Domain.Payments.Repositories;
using Fiap.CloudGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fiap.CloudGames.Infrastructure.Payments.Repositories;

public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    private readonly AppDbContext _db = db;

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct) => 
        await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct) => 
        await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

    public async Task<Payment?> GetByPaymentTransactionId(string paymentTransactionId, CancellationToken ct) => 
        await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(o => o.PaymentTransactionId == paymentTransactionId, ct);

    public async Task<(IReadOnlyList<Payment> Items, int Total)> QueryAllAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.Payments.AsNoTracking();

        if (startDate.HasValue) q = q.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(o => o.CreatedAt <= endDate.Value);

        if (TryParseStatus(status, out var st))
            q = q.Where(o => o.Status == st);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Payment> Items, int Total)> QueryForUserAsync(
        string userEmail,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.Payments.AsNoTracking()
            .Where(o => o.UserEmail == userEmail);

        if (TryParseStatus(status, out var st))
            q = q.Where(o => o.Status == st);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Payment payment, CancellationToken ct)
    {
        await _db.Payments.AddAsync(payment, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var payment = await _db.Payments.FindAsync(new object[] { id }, ct);
        if (payment is null) return;
        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync(ct);
    }

    private static bool TryParseStatus(string? input, out PaymentStatus status)
    {
        if (!string.IsNullOrWhiteSpace(input) &&
            Enum.TryParse<PaymentStatus>(input, true, out var parsed))
        {
            status = parsed;
            return true;
        }
        status = default;
        return false;
    }
}
