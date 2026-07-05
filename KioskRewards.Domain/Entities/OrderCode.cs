using KioskRewards.Domain.Exceptions;

namespace KioskRewards.Domain.Entities;

/// <summary>
/// A one-time-claimable code representing a real-world company order (e.g. "100 packs of cigarettes"),
/// worth a fixed number of points. Simulates the company's own order system, which this practice
/// project has no real integration point for - see docs/PROJECT-CONTEXT.md.
/// </summary>
public class OrderCode
{
    // EF Core materialisation constructor.
    private OrderCode() { }

    private OrderCode(string code, string productDescription, int pointsValue)
    {
        Code = code;
        ProductDescription = productDescription;
        PointsValue = pointsValue;
    }

    public int Id { get; private set; }

    /// Normalized (trimmed, upper-invariant) so kiosk-owner typos in casing still match.
    public string Code { get; private set; } = string.Empty;

    public string ProductDescription { get; private set; } = string.Empty;

    public int PointsValue { get; private set; }

    /// Null until claimed - the member who redeemed this code for points.
    public Guid? ClaimedByMemberKey { get; private set; }

    public DateTime? ClaimedUtc { get; private set; }

    /// Optimistic-concurrency token: guards against two claims racing on the same code
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public bool IsClaimed => ClaimedByMemberKey is not null;

    public static OrderCode Create(string code, string productDescription, int pointsValue)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Order code is required.", nameof(code));
        if (pointsValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(pointsValue), "Points value must be positive.");

        return new OrderCode(Normalize(code), productDescription, pointsValue);
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public void Claim(Guid memberKey, DateTime nowUtc)
    {
        if (IsClaimed)
            throw new OrderCodeAlreadyClaimedException(Code);

        ClaimedByMemberKey = memberKey;
        ClaimedUtc = nowUtc;
    }
}
