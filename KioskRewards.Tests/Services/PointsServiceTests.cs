using KioskRewards.Domain.Enums;
using KioskRewards.Infrastructure.Services;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Integration tests for PointsService against a real SQLite db, to check the service + EF mapping
/// + aggregate all actually work together end to end.
/// </summary>
public class PointsServiceTests : SqliteTestBase
{
    private static readonly Guid Member = Guid.Parse("c3c3c3c3-0000-0000-0000-000000000003");

    [Fact]
    public async Task GetBalance_returns_zero_for_unknown_member()
    {
        var svc = new PointsService(Db);

        Assert.Equal(0, await svc.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task Earn_creates_account_on_first_use_and_accumulates_balance()
    {
        var svc = new PointsService(Db);

        var r1 = await svc.EarnAsync(Member, 100, "sale 1");
        var r2 = await svc.EarnAsync(Member, 50, "sale 2");

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.Equal(150, await svc.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task Redeem_with_enough_points_decreases_balance_and_records_history()
    {
        var svc = new PointsService(Db);
        await svc.EarnAsync(Member, 200, "earn");

        var result = await svc.RedeemAsync(Member, 80, "Coffee mug");

        Assert.True(result.IsSuccess);
        Assert.Equal(120, await svc.GetBalanceAsync(Member));

        var history = await svc.GetHistoryAsync(Member);
        Assert.Equal(2, history.Count);
        Assert.Equal(TransactionType.Redeem, history[0].Type);   // newest first
        Assert.Equal(80, history[0].Amount);
        Assert.Equal("Coffee mug", history[0].Description);
    }

    [Fact]
    public async Task Redeem_without_an_account_fails()
    {
        var svc = new PointsService(Db);

        var result = await svc.RedeemAsync(Member, 10, "anything");

        Assert.True(result.IsFailure);
        Assert.Equal(0, await svc.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task Redeem_more_than_balance_fails_and_leaves_balance_intact()
    {
        var svc = new PointsService(Db);
        await svc.EarnAsync(Member, 30, "earn");

        var result = await svc.RedeemAsync(Member, 100, "too expensive");

        Assert.True(result.IsFailure);
        Assert.Equal(30, await svc.GetBalanceAsync(Member));
        Assert.Single(await svc.GetHistoryAsync(Member));   // no redeem entry written
    }

    [Fact]
    public async Task History_persists_across_scopes_and_is_ordered_newest_first()
    {
        // write using one context...
        var writer = new PointsService(Db);
        await writer.EarnAsync(Member, 100, "first");
        await writer.RedeemAsync(Member, 40, "second");

        // ...then read it back with a totally new context on the same db
        using var ctx = NewContext();
        var reader = new PointsService(ctx);
        var history = await reader.GetHistoryAsync(Member);

        Assert.Equal(2, history.Count);
        Assert.Equal("second", history[0].Description);
        Assert.Equal("first", history[1].Description);
    }
}
