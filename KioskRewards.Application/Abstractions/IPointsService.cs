using KioskRewards.Application.DTOs;
using KioskRewards.Domain.Common;

namespace KioskRewards.Application.Abstractions;

/// <summary>
/// Everything the presentation layer can do with loyalty points, without needing to know anything
/// about EF Core or Umbraco underneath.
/// </summary>
public interface IPointsService
{
    /// Current balance, or 0 if they don't have an account yet
    Task<int> GetBalanceAsync(Guid memberKey, CancellationToken ct = default);

    /// Transaction history, newest first
    Task<IReadOnlyList<PointsTransactionDto>> GetHistoryAsync(Guid memberKey, CancellationToken ct = default);

    /// <summary>
    /// Hands out points, creating the account if this is the member's first time earning.
    /// </summary>
    Task<Result> EarnAsync(Guid memberKey, int amount, string description, CancellationToken ct = default);

    /// <summary>
    /// Spends points on a reward. Comes back as a failed Result for the expected failure cases
    /// (no account, not enough points) rather than throwing.
    /// </summary>
    Task<Result> RedeemAsync(Guid memberKey, int cost, string description, CancellationToken ct = default);
}
