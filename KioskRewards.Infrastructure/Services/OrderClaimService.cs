using KioskRewards.Application.Abstractions;
using KioskRewards.Application.DTOs;
using KioskRewards.Domain.Common;
using KioskRewards.Domain.Entities;
using KioskRewards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Services;

/// <summary>
/// Claiming an order code touches two aggregates - the OrderCode itself (marks it claimed) and the
/// member's PointsAccount (awards the points). Deliberately does NOT go through IPointsService.EarnAsync
/// for this: that method does its own SaveChanges, which would mean two separate commits for what is
/// really one business operation. Instead this talks to the domain methods directly (same idiom as
/// PointsService) so both changes land in a single SaveChangesAsync - a code can never end up marked
/// claimed without the points actually landing, or vice versa.
/// </summary>
public sealed class OrderClaimService : IOrderClaimService
{
    private readonly LoyaltyDbContext _db;

    public OrderClaimService(LoyaltyDbContext db) => _db = db;

    public async Task<Result<OrderClaimResultDto>> ClaimAsync(Guid memberKey, string code, CancellationToken ct = default)
    {
        var normalizedCode = OrderCode.Normalize(code);

        var order = await _db.OrderCodes.FirstOrDefaultAsync(o => o.Code == normalizedCode, ct);
        if (order is null)
            return Result.Failure<OrderClaimResultDto>("That order code was not found.");

        if (order.IsClaimed)
            return Result.Failure<OrderClaimResultDto>("This order code has already been claimed.");

        var now = DateTime.UtcNow;

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.MemberKey == memberKey, ct);
        if (account is null)
        {
            account = PointsAccount.Create(memberKey);
            await _db.PointsAccounts.AddAsync(account, ct);
        }

        account.Earn(order.PointsValue, $"Order code {order.Code}: {order.ProductDescription}", now);
        order.Claim(memberKey, now);

        try
        {
            // one SaveChanges covers both the order-code claim and the points award
            await _db.SaveChangesAsync(ct);
            return Result.Success(new OrderClaimResultDto(order.ProductDescription, order.PointsValue));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<OrderClaimResultDto>("Something changed at the same time. Please try again.");
        }
    }
}
