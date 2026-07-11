using KioskRewards.Infrastructure.Services;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Integration tests for ReportingService against a real SQLite db - written through PointsService so
/// the ledger rows look exactly like what production code actually writes.
/// </summary>
public class ReportingServiceTests : SqliteTestBase
{
    private static readonly Guid KioskA = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000001");
    private static readonly Guid KioskB = Guid.Parse("d2d2d2d2-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetReport_with_no_transactions_returns_zeroes_and_empty_lists()
    {
        var svc = new ReportingService(Db);

        var report = await svc.GetReportAsync();

        Assert.Equal(0, report.TotalPointsEarned);
        Assert.Equal(0, report.TotalPointsRedeemed);
        Assert.Equal(0, report.NetPointsOutstanding);
        Assert.Empty(report.TopRewards);
        Assert.Empty(report.TopKiosks);
    }

    [Fact]
    public async Task GetReport_sums_earned_and_redeemed_across_all_kiosks()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 100, "welcome bonus");
        await points.EarnAsync(KioskB, 200, "welcome bonus");
        await points.RedeemAsync(KioskA, 30, "Redeemed: Lighter");

        var report = await new ReportingService(Db).GetReportAsync();

        Assert.Equal(300, report.TotalPointsEarned);
        Assert.Equal(30, report.TotalPointsRedeemed);
        Assert.Equal(270, report.NetPointsOutstanding);
    }

    [Fact]
    public async Task GetReport_groups_top_rewards_by_description_with_count_and_points_spent()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 500, "welcome bonus");
        await points.EarnAsync(KioskB, 500, "welcome bonus");
        await points.RedeemAsync(KioskA, 50, "Redeemed: Lighter");
        await points.RedeemAsync(KioskB, 50, "Redeemed: Lighter");
        await points.RedeemAsync(KioskA, 150, "Redeemed: Ashtray");

        var report = await new ReportingService(Db).GetReportAsync();

        Assert.Equal(2, report.TopRewards.Count);
        var lighter = report.TopRewards.Single(r => r.Description == "Redeemed: Lighter");
        Assert.Equal(2, lighter.RedemptionCount);
        Assert.Equal(100, lighter.TotalPointsSpent);
        Assert.Equal("Redeemed: Lighter", report.TopRewards[0].Description); // most-redeemed first
    }

    [Fact]
    public async Task GetReport_ranks_top_kiosks_by_points_earned_and_tracks_their_redemptions()
    {
        var points = new PointsService(Db);
        await points.EarnAsync(KioskA, 1000, "welcome bonus");
        await points.EarnAsync(KioskB, 100, "welcome bonus");
        await points.RedeemAsync(KioskA, 200, "Redeemed: Giftcard");

        var report = await new ReportingService(Db).GetReportAsync();

        Assert.Equal(KioskA, report.TopKiosks[0].MemberKey);
        Assert.Equal(1000, report.TopKiosks[0].TotalPointsEarned);
        Assert.Equal(200, report.TopKiosks[0].TotalPointsRedeemed);
    }

    [Fact]
    public async Task GetReport_respects_topCount_limit()
    {
        var points = new PointsService(Db);
        for (var i = 0; i < 7; i++)
            await points.EarnAsync(Guid.NewGuid(), 10, "welcome bonus");

        var report = await new ReportingService(Db).GetReportAsync(topCount: 3);

        Assert.Equal(3, report.TopKiosks.Count);
    }
}
