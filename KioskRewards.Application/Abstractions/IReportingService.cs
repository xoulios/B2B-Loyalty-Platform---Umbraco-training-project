using KioskRewards.Application.DTOs;

namespace KioskRewards.Application.Abstractions;

/// <summary>
/// Company-wide loyalty stats, for the "Company Admin" reporting page - separate from IPointsService,
/// which only ever answers questions about a single member.
/// </summary>
public interface IReportingService
{
    /// <param name="topCount">How many entries to return in the TopRewards/TopKiosks lists.</param>
    Task<CompanyReportDto> GetReportAsync(int topCount = 5, CancellationToken ct = default);
}
