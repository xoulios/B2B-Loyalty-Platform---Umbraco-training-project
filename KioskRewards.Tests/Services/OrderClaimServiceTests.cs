using KioskRewards.Domain.Entities;
using KioskRewards.Infrastructure.Services;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Integration tests for OrderClaimService against a real SQLite db, to check the service + EF mapping
/// + both aggregates (OrderCode, PointsAccount) actually work together end to end.
/// </summary>
public class OrderClaimServiceTests : SqliteTestBase
{
    private static readonly Guid Member = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000004");

    private async Task SeedOrderAsync(string code, string description, int points)
    {
        Db.OrderCodes.Add(OrderCode.Create(code, description, points));
        await Db.SaveChangesAsync();
    }

    [Fact]
    public async Task ClaimAsync_with_a_valid_unclaimed_code_awards_points_and_records_history()
    {
        await SeedOrderAsync("SALE-100PK-CIG", "100 packs of cigarettes", 250);
        var svc = new OrderClaimService(Db);
        var points = new PointsService(Db);

        var result = await svc.ClaimAsync(Member, "sale-100pk-cig");   // lower case on purpose - should still match

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Value.PointsAwarded);
        Assert.Equal("100 packs of cigarettes", result.Value.ProductDescription);
        Assert.Equal(250, await points.GetBalanceAsync(Member));

        var history = await points.GetHistoryAsync(Member);
        Assert.Single(history);
        Assert.Contains("SALE-100PK-CIG", history[0].Description);
    }

    [Fact]
    public async Task ClaimAsync_marks_the_code_as_claimed_so_it_cannot_be_reused()
    {
        await SeedOrderAsync("CODE1", "desc", 100);
        var svc = new OrderClaimService(Db);

        var first = await svc.ClaimAsync(Member, "CODE1");
        var second = await svc.ClaimAsync(Member, "CODE1");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);

        var points = new PointsService(Db);
        Assert.Equal(100, await points.GetBalanceAsync(Member));   // not double-awarded
    }

    [Fact]
    public async Task ClaimAsync_with_an_unknown_code_fails_and_awards_nothing()
    {
        var svc = new OrderClaimService(Db);

        var result = await svc.ClaimAsync(Member, "DOES-NOT-EXIST");

        Assert.True(result.IsFailure);

        var points = new PointsService(Db);
        Assert.Equal(0, await points.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task ClaimAsync_creates_an_account_on_the_member_s_first_claim()
    {
        await SeedOrderAsync("CODE1", "desc", 60);
        var svc = new OrderClaimService(Db);

        var result = await svc.ClaimAsync(Member, "CODE1");

        Assert.True(result.IsSuccess);

        var points = new PointsService(Db);
        Assert.Equal(60, await points.GetBalanceAsync(Member));
    }
}
