namespace KioskRewards.Domain.Entities;

/// <summary>
/// A record that a specific company order (identified by the Key of its Umbraco "CompanyOrder"
/// content node) has been claimed by a member for points. The code/description/points themselves
/// live in Umbraco content, not here - this row's only job is to answer "has this one already been
/// used": no row for a given ContentKey means "not yet claimed". Insert-only, never updated, so
/// unlike PointsAccount there's no RowVersion here - there's nothing to race an UPDATE against, only
/// a duplicate INSERT, which the unique index on ContentKey (see OrderCodeConfiguration) catches.
/// </summary>
public class OrderCode
{
    // EF Core materialisation constructor.
    private OrderCode() { }

    private OrderCode(Guid contentKey, Guid memberKey, DateTime claimedUtc)
    {
        ContentKey = contentKey;
        ClaimedByMemberKey = memberKey;
        ClaimedUtc = claimedUtc;
    }

    public int Id { get; private set; }

    /// The Key of the Umbraco "CompanyOrder" content node this claim is for.
    public Guid ContentKey { get; private set; }

    public Guid ClaimedByMemberKey { get; private set; }

    public DateTime ClaimedUtc { get; private set; }

    public static OrderCode CreateClaim(Guid contentKey, Guid memberKey, DateTime nowUtc)
    {
        if (contentKey == Guid.Empty)
            throw new ArgumentException("Content key is required.", nameof(contentKey));
        if (memberKey == Guid.Empty)
            throw new ArgumentException("Member key is required.", nameof(memberKey));

        return new OrderCode(contentKey, memberKey, nowUtc);
    }
}
