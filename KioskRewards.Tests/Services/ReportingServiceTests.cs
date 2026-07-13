using KioskRewards.Infrastructure.Services;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Integration tests for ReportingService against a real SQLite db - written through PointsService so
/// the ledger rows look exactly like what production code actually writes. Everything is scoped to a
/// single MemberKey - a kiosk owner's report must never include another kiosk's activity.
/// </summary>
public class ReportingServiceTests : SqliteTestBase
{
    private static readonly Guid KioskA = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000001");
    private static readonly Guid KioskB = Guid.Parse("d2d2d2d2-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetReport_with_no_transactions_returns_zeroes_and_empty_list()
    {
        var svc = new ReportingService(Db);

        var report = await svc.GetReportAsync(KioskA);

        Assert.Equal(0, report.TotalPointsEarned);
        Assert.Equal(0, report.TotalPointsRedeemed);
        Assert.Equal(0, report.NetPointsOutstanding);
        Assert.Empty(report.TopRewards);
    }

    [Fact]
    public async Task GetReport_only_sums_the_requested_kiosks_own_transactions()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 100, "welcome bonus");
        await points.RedeemAsync(KioskA, 30, "Redeemed: Lighter");
        // KioskB's activity must not leak into KioskA's report
        await points.EarnAsync(KioskB, 9000, "welcome bonus");
        await points.RedeemAsync(KioskB, 500, "Redeemed: Giftcard");

        var report = await new ReportingService(Db).GetReportAsync(KioskA);

        Assert.Equal(100, report.TotalPointsEarned);
        Assert.Equal(30, report.TotalPointsRedeemed);
        Assert.Equal(70, report.NetPointsOutstanding);
    }

    [Fact]
    public async Task GetReport_groups_top_rewards_by_description_with_count_and_points_spent()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 500, "welcome bonus");
        await points.RedeemAsync(KioskA, 50, "Redeemed: Lighter");
        await points.RedeemAsync(KioskA, 50, "Redeemed: Lighter");
        await points.RedeemAsync(KioskA, 150, "Redeemed: Ashtray");
        // a different kiosk redeeming the same reward name must not inflate KioskA's count
        await points.EarnAsync(KioskB, 500, "welcome bonus");
        await points.RedeemAsync(KioskB, 50, "Redeemed: Lighter");

        var report = await new ReportingService(Db).GetReportAsync(KioskA);

        Assert.Equal(2, report.TopRewards.Count);
        var lighter = report.TopRewards.Single(r => r.Description == "Redeemed: Lighter");
        Assert.Equal(2, lighter.RedemptionCount); // only KioskA's two, not KioskB's
        Assert.Equal(100, lighter.TotalPointsSpent);
        Assert.Equal("Redeemed: Lighter", report.TopRewards[0].Description); // most-redeemed first
    }

    [Fact]
    public async Task GetReport_respects_topCount_limit()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 1000, "welcome bonus");
        for (var i = 0; i < 7; i++)
            await points.RedeemAsync(KioskA, 10, $"Redeemed: Item{i}");

        var report = await new ReportingService(Db).GetReportAsync(KioskA, topCount: 3);

        Assert.Equal(3, report.TopRewards.Count);
    }
}
