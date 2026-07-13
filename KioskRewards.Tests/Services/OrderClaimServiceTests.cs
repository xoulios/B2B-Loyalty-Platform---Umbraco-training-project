using KioskRewards.Domain.Common;
using KioskRewards.Infrastructure.Services;

namespace KioskRewards.Tests.Services;

/// <summary>
/// Integration tests for OrderClaimService against a real SQLite db. The Umbraco content lookup
/// itself lives in OrderClaimController now (see docs/PROJECT-CONTEXT.md) - this service only ever
/// sees plain values (a content key, a points value, a description), so these tests just make those
/// values up directly instead of needing any Umbraco content.
/// </summary>
public class OrderClaimServiceTests : SqliteTestBase
{
    private static readonly Guid Member = Guid.Parse("d4d4d4d4-0000-0000-0000-000000000004");
    private static readonly Guid ContentKey = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public async Task ClaimAsync_with_an_unclaimed_content_key_awards_points_and_records_history()
    {
        var svc = new OrderClaimService(Db);
        var points = new PointsService(Db);

        var result = await svc.ClaimAsync(Member, ContentKey, 250, "Order code SALE-100PK-CIG: 100 packs of cigarettes");

        Assert.True(result.IsSuccess);
        Assert.Equal(250, await points.GetBalanceAsync(Member));

        var history = await points.GetHistoryAsync(Member, new PagedQuery());
        Assert.Equal(1, history.TotalCount);
        Assert.Contains("SALE-100PK-CIG", history.Items[0].Description);
    }

    [Fact]
    public async Task ClaimAsync_twice_for_the_same_content_key_fails_the_second_time_and_does_not_double_award()
    {
        var svc = new OrderClaimService(Db);

        var first = await svc.ClaimAsync(Member, ContentKey, 100, "desc");
        var second = await svc.ClaimAsync(Member, ContentKey, 100, "desc");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);

        var points = new PointsService(Db);
        Assert.Equal(100, await points.GetBalanceAsync(Member));   // not double-awarded
    }

    [Fact]
    public async Task ClaimAsync_creates_an_account_on_the_member_s_first_claim()
    {
        var svc = new OrderClaimService(Db);

        var result = await svc.ClaimAsync(Member, ContentKey, 60, "desc");

        Assert.True(result.IsSuccess);

        var points = new PointsService(Db);
        Assert.Equal(60, await points.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task ClaimAsync_for_different_content_keys_awards_both_independently()
    {
        var svc = new OrderClaimService(Db);
        var otherContentKey = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

        var first = await svc.ClaimAsync(Member, ContentKey, 250, "first order");
        var second = await svc.ClaimAsync(Member, otherContentKey, 125, "second order");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var points = new PointsService(Db);
        Assert.Equal(375, await points.GetBalanceAsync(Member));
    }

    [Fact]
    public async Task IsClaimedAsync_is_false_for_a_content_key_nobody_has_claimed_yet()
    {
        var svc = new OrderClaimService(Db);

        Assert.False(await svc.IsClaimedAsync(ContentKey));
    }

    [Fact]
    public async Task IsClaimedAsync_is_true_once_that_content_key_has_been_claimed()
    {
        var svc = new OrderClaimService(Db);
        await svc.ClaimAsync(Member, ContentKey, 100, "desc");

        Assert.True(await svc.IsClaimedAsync(ContentKey));
    }
}
