using KioskRewards.Domain.Entities;

namespace KioskRewards.Tests.Domain;

/// <summary>
/// Pure domain tests, no db, no Umbraco - just checking the claim record enforces its own rules.
/// </summary>
public class OrderCodeTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ContentKey = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Member = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CreateClaim_records_who_claimed_it_and_when()
    {
        var claim = OrderCode.CreateClaim(ContentKey, Member, Now);

        Assert.Equal(ContentKey, claim.ContentKey);
        Assert.Equal(Member, claim.ClaimedByMemberKey);
        Assert.Equal(Now, claim.ClaimedUtc);
    }

    [Fact]
    public void CreateClaim_with_empty_content_key_throws()
    {
        Assert.Throws<ArgumentException>(() => OrderCode.CreateClaim(Guid.Empty, Member, Now));
    }

    [Fact]
    public void CreateClaim_with_empty_member_key_throws()
    {
        Assert.Throws<ArgumentException>(() => OrderCode.CreateClaim(ContentKey, Guid.Empty, Now));
    }
}
