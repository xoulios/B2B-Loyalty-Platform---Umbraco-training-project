using KioskRewards.Application.Abstractions;
using KioskRewards.Application.DTOs;
using KioskRewards.Domain.Enums;
using KioskRewards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KioskRewards.Infrastructure.Services;

/// <summary>
/// Runs aggregate queries straight against the ledger - no domain logic here, just SUM/GROUP BY over
/// the same PointsTransactions table PointsService already writes to.
/// </summary>
public sealed class ReportingService : IReportingService
{
    private readonly LoyaltyDbContext _db;

    public ReportingService(LoyaltyDbContext db) => _db = db;

    public async Task<CompanyReportDto> GetReportAsync(int topCount = 5, CancellationToken ct = default)
    {
        var totalEarned = await _db.PointsTransactions
            .Where(t => t.Type == TransactionType.Earn)
            .SumAsync(t => t.Amount, ct);

        var totalRedeemed = await _db.PointsTransactions
            .Where(t => t.Type == TransactionType.Redeem)
            .SumAsync(t => t.Amount, ct);

        // EF Core can't translate an ORDER BY applied after projecting into a record's constructor -
        // project into an anonymous type (which it understands natively) and build the DTOs in memory
        // once the (already top-N, already ordered) rows are materialized.
        var topRewardsRaw = await _db.PointsTransactions
            .Where(t => t.Type == TransactionType.Redeem)
            .GroupBy(t => t.Description)
            .Select(g => new { Description = g.Key, Count = g.Count(), Total = g.Sum(t => t.Amount) })
            .OrderByDescending(r => r.Count)
            .ThenByDescending(r => r.Total)
            .Take(topCount)
            .ToListAsync(ct);
        var topRewards = topRewardsRaw
            .Select(r => new TopRewardDto(r.Description, r.Count, r.Total))
            .ToList();

        var topKiosksRaw = await _db.PointsTransactions
            .GroupBy(t => t.MemberKey)
            .Select(g => new
            {
                MemberKey = g.Key,
                Earned = g.Sum(t => t.Type == TransactionType.Earn ? t.Amount : 0),
                Redeemed = g.Sum(t => t.Type == TransactionType.Redeem ? t.Amount : 0)
            })
            .OrderByDescending(k => k.Earned)
            .Take(topCount)
            .ToListAsync(ct);
        var topKiosks = topKiosksRaw
            .Select(k => new TopKioskDto(k.MemberKey, k.Earned, k.Redeemed))
            .ToList();

        return new CompanyReportDto(totalEarned, totalRedeemed, topRewards, topKiosks);
    }
}
