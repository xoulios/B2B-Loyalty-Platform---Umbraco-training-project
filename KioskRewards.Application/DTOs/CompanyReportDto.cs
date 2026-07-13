namespace KioskRewards.Application.DTOs;

/// <summary>
/// A single kiosk owner's own loyalty report - scoped to their MemberKey only. Never aggregates
/// across kiosks: different kiosk owners are independent businesses on this platform, so one owner's
/// dashboard must never leak another's activity or ranking.
/// </summary>
public sealed record CompanyReportDto(
    int TotalPointsEarned,
    int TotalPointsRedeemed,
    IReadOnlyList<TopRewardDto> TopRewards)
{
    /// Points earned but not yet spent - this kiosk's current outstanding balance.
    public int NetPointsOutstanding => TotalPointsEarned - TotalPointsRedeemed;
}

/// <summary>
/// One reward's redemption stats for this kiosk, grouped by the transaction description recorded at
/// redeem time (e.g. "Redeemed: Lighter") - there's no direct FK from PointsTransaction to a Reward
/// content node.
/// </summary>
public sealed record TopRewardDto(string Description, int RedemptionCount, int TotalPointsSpent);
