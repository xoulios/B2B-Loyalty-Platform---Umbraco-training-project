using KioskRewards.Application.Abstractions;
using KioskRewards.Application.DTOs;
using KioskRewards.Domain.Common;
using KioskRewards.Domain.Entities;
using KioskRewards.Infrastructure.Common;
using KioskRewards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Services;

/// <summary>
/// Loads/saves the PointsAccount aggregate and hands the actual rules off to it - this class doesn't
/// do any business logic itself, just plumbing. Also where "now" gets read and passed into the domain.
/// </summary>
public sealed class PointsService : IPointsService
{
    private readonly LoyaltyDbContext _db;

    public PointsService(LoyaltyDbContext db) => _db = db;

    public async Task<int> GetBalanceAsync(Guid memberKey, CancellationToken ct = default)
    {
        var account = await _db.PointsAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.MemberKey == memberKey, ct);

        return account?.Balance ?? 0;
    }

    public async Task<PagedResult<PointsTransactionDto>> GetHistoryAsync(Guid memberKey, PagedQuery query, CancellationToken ct = default)
    {
        return await _db.PointsTransactions
            .AsNoTracking()
            .Where(t => t.MemberKey == memberKey)
            .OrderByDescending(t => t.CreatedUtc)
            .ThenByDescending(t => t.Id)
            .Select(t => new PointsTransactionDto(t.Amount, t.Type, t.Description, t.CreatedUtc))
            .ToPagedResultAsync(query, ct);
    }

    public async Task<Result> EarnAsync(Guid memberKey, int amount, string description, CancellationToken ct = default)
    {
        if (amount <= 0)
            return Result.Failure("Earned points must be positive.");

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.MemberKey == memberKey, ct);
        if (account is null)
        {
            account = PointsAccount.Create(memberKey);
            await _db.PointsAccounts.AddAsync(account, ct);
        }

        account.Earn(amount, description, DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RedeemAsync(Guid memberKey, int cost, string description, CancellationToken ct = default)
    {
        if (cost <= 0)
            return Result.Failure("Redeem cost must be positive.");

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.MemberKey == memberKey, ct);
        if (account is null)
            return Result.Failure("No loyalty account found for this member.");

        if (!account.CanRedeem(cost))
            return Result.Failure($"Insufficient points: balance is {account.Balance}, but {cost} is required.");

        account.Redeem(cost, description, DateTime.UtcNow);

        try
        {
            // one SaveChanges covers both the account update and the new transaction row
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("Your balance changed at the same time. Please try again.");
        }
    }
}
