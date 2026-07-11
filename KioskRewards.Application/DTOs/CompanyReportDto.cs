namespace KioskRewards.Application.DTOs;

/// <summary>
/// Company-wide aggregate view over the loyalty ledger - everything a business owner would want to
/// see that a single member's balance/history can't show.
/// </summary>
public sealed record CompanyReportDto(
    int TotalPointsEarned,
    int TotalPointsRedeemed,
    IReadOnlyList<TopRewardDto> TopRewards,
    IReadOnlyList<TopKioskDto> TopKiosks)
{
    /// Points handed out but not yet spent - the company's outstanding "liability" in points.
    public int NetPointsOutstanding => TotalPointsEarned - TotalPointsRedeemed;
}

/// <summary>
/// One reward's redemption stats, grouped by the transaction description recorded at redeem time
/// (e.g. "Redeemed: Lighter") - there's no direct FK from PointsTransaction to a Reward content node.
/// </summary>
public sealed record TopRewardDto(string Description, int RedemptionCount, int TotalPointsSpent);

/// One kiosk's activity, identified by their loyalty MemberKey (Umbraco Member.Key)
public sealed record TopKioskDto(Guid MemberKey, int TotalPointsEarned, int TotalPointsRedeemed);
