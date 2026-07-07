using KioskRewards.Application.Abstractions;
using KioskRewards.Domain.Common;
using KioskRewards.Domain.Entities;
using KioskRewards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Services;

/// <summary>
/// Claiming an order code touches two aggregates - a new OrderCode claim row gets inserted, and the
/// member's PointsAccount earns points. Deliberately does NOT go through IPointsService.EarnAsync for
/// this: that method does its own SaveChanges, which would mean two separate commits for what is
/// really one business operation. Instead this talks to the domain methods directly (same idiom as
/// PointsService) so both changes land in a single SaveChangesAsync - a claim can never end up
/// recorded without the points actually landing, or vice versa.
/// </summary>
public sealed class OrderClaimService : IOrderClaimService
{
    private readonly LoyaltyDbContext _db;

    public OrderClaimService(LoyaltyDbContext db) => _db = db;

    public async Task<Result> ClaimAsync(Guid memberKey, Guid orderContentKey, int pointsValue, string description, CancellationToken ct = default)
    {
        var alreadyClaimed = await _db.OrderCodes.AnyAsync(o => o.ContentKey == orderContentKey, ct);
        if (alreadyClaimed)
            return Result.Failure("This order code has already been claimed.");

        var now = DateTime.UtcNow;

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.MemberKey == memberKey, ct);
        if (account is null)
        {
            account = PointsAccount.Create(memberKey);
            await _db.PointsAccounts.AddAsync(account, ct);
        }

        account.Earn(pointsValue, description, now);
        await _db.OrderCodes.AddAsync(OrderCode.CreateClaim(orderContentKey, memberKey, now), ct);

        try
        {
            // one SaveChanges covers both the new claim row and the points award
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            // the unique index on ContentKey caught a race: two requests claimed the same code at once
            return Result.Failure("This order code has already been claimed.");
        }
    }

    public Task<bool> IsClaimedAsync(Guid orderContentKey, CancellationToken ct = default) =>
        _db.OrderCodes.AnyAsync(o => o.ContentKey == orderContentKey, ct);
}
