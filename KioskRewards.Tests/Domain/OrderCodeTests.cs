using KioskRewards.Domain.Entities;
using KioskRewards.Domain.Exceptions;

namespace KioskRewards.Tests.Domain;

/// <summary>
/// Pure domain tests, no db, no Umbraco - just checking the aggregate enforces its own rules.
/// </summary>
public class OrderCodeTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Member = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_normalizes_the_code_to_trimmed_upper_invariant()
    {
        var order = OrderCode.Create("  sale-100pk-cig  ", "100 packs", 250);

        Assert.Equal("SALE-100PK-CIG", order.Code);
    }

    [Fact]
    public void Create_starts_unclaimed()
    {
        var order = OrderCode.Create("CODE1", "desc", 100);

        Assert.False(order.IsClaimed);
        Assert.Null(order.ClaimedByMemberKey);
        Assert.Null(order.ClaimedUtc);
    }

    [Fact]
    public void Create_with_blank_code_throws()
    {
        Assert.Throws<ArgumentException>(() => OrderCode.Create("   ", "desc", 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Create_with_non_positive_points_throws(int points)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderCode.Create("CODE1", "desc", points));
    }

    [Fact]
    public void Claim_marks_the_code_as_claimed_by_that_member()
    {
        var order = OrderCode.Create("CODE1", "desc", 100);

        order.Claim(Member, Now);

        Assert.True(order.IsClaimed);
        Assert.Equal(Member, order.ClaimedByMemberKey);
        Assert.Equal(Now, order.ClaimedUtc);
    }

    [Fact]
    public void Claim_twice_throws_and_leaves_the_original_claim_intact()
    {
        var order = OrderCode.Create("CODE1", "desc", 100);
        order.Claim(Member, Now);

        var otherMember = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var later = Now.AddDays(1);

        var ex = Assert.Throws<OrderCodeAlreadyClaimedException>(() => order.Claim(otherMember, later));

        Assert.Equal("CODE1", ex.Code);
        Assert.Equal(Member, order.ClaimedByMemberKey);   // unchanged - still the first claimant
        Assert.Equal(Now, order.ClaimedUtc);
    }
}
