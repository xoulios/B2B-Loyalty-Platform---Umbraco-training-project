using KioskRewards.Application.DTOs;

namespace KioskRewards.Application.Abstractions;

/// <summary>
/// A kiosk owner's own loyalty report - separate from IPointsService's raw balance/history, this
/// summarizes/ranks their own activity (e.g. top redeemed rewards). Always scoped to one MemberKey.
/// </summary>
public interface IReportingService
{
    /// <param name="topCount">How many entries to return in the TopRewards list.</param>
    Task<CompanyReportDto> GetReportAsync(Guid memberKey, int topCount = 5, CancellationToken ct = default);
}
