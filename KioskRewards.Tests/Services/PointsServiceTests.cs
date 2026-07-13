using KioskRewards.Domain.Common;
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

        var history = await svc.GetHistoryAsync(Member, new PagedQuery());
        Assert.Equal(2, history.TotalCount);
        Assert.Equal(TransactionType.Redeem, history.Items[0].Type);   // newest first
        Assert.Equal(80, history.Items[0].Amount);
        Assert.Equal("Coffee mug", history.Items[0].Description);
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
        Assert.Equal(1, (await svc.GetHistoryAsync(Member, new PagedQuery())).TotalCount);   // no redeem entry written
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
        var history = await reader.GetHistoryAsync(Member, new PagedQuery());

        Assert.Equal(2, history.TotalCount);
        Assert.Equal("second", history.Items[0].Description);
        Assert.Equal("first", history.Items[1].Description);
    }

    [Fact]
    public async Task GetHistoryAsync_slices_into_pages_and_reports_correct_metadata()
    {
        var svc = new PointsService(Db);
        await svc.EarnAsync(Member, 10, "first");
        await svc.EarnAsync(Member, 20, "second");
        await svc.EarnAsync(Member, 30, "third");   // newest

        var page1 = await svc.GetHistoryAsync(Member, new PagedQuery(Page: 1, PageSize: 2));
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);
        Assert.Equal("third", page1.Items[0].Description);   // still newest first
        Assert.Equal("second", page1.Items[1].Description);

        var page2 = await svc.GetHistoryAsync(Member, new PagedQuery(Page: 2, PageSize: 2));
        Assert.Single(page2.Items);
        Assert.Equal("first", page2.Items[0].Description);
        Assert.False(page2.HasNextPage);
        Assert.True(page2.HasPreviousPage);
    }
}
