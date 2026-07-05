using KioskRewards.Application.DTOs;
using KioskRewards.Domain.Common;

namespace KioskRewards.Application.Abstractions;

/// <summary>
/// The "enter an order code, get the points it's worth" flow - simulates checking the code against
/// the company's own order system (which doesn't really exist here, see docs/PROJECT-CONTEXT.md).
/// </summary>
public interface IOrderClaimService
{
    /// <summary>
    /// Looks up the code, and if it exists and hasn't been claimed yet, awards its points to the
    /// member and marks it claimed - both in one atomic operation, so a code can never be marked
    /// claimed without the matching points actually landing (or vice versa).
    /// Comes back as a failed Result for the expected failure cases (code not found, already
    /// claimed) rather than throwing.
    /// </summary>
    Task<Result<OrderClaimResultDto>> ClaimAsync(Guid memberKey, string code, CancellationToken ct = default);
}
