using KioskRewards.Domain.Common;

namespace KioskRewards.Application.Abstractions;

/// <summary>
/// The "enter an order code, get the points it's worth" flow. Deliberately knows nothing about
/// Umbraco content - the caller (a controller, which is allowed to talk to Umbraco) already resolved
/// the code to a content node and hands over just its Key, points value, and a ready-to-store
/// description. Keeps this service (and the whole loyalty core underneath it) 100% Umbraco-agnostic,
/// same principle as IPointsService.
/// </summary>
public interface IOrderClaimService
{
    /// <summary>
    /// Awards the points to the member and records the claim - both in one atomic operation, so a
    /// content node can never end up "claimed" without the matching points actually landing (or vice
    /// versa). Comes back as a failed Result for the expected failure case (already claimed) rather
    /// than throwing.
    /// </summary>
    Task<Result> ClaimAsync(Guid memberKey, Guid orderContentKey, int pointsValue, string description, CancellationToken ct = default);
}
