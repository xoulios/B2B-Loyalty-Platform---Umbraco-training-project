namespace KioskRewards.Domain.Common;

/// <summary>
/// Parameter object for "give me page N, this many items" - keeps GetHistoryAsync from growing a
/// pile of loose int arguments (same idea as VideoClubV2's PagedQuery).
/// </summary>
public sealed record PagedQuery(int Page = 1, int PageSize = 10);
