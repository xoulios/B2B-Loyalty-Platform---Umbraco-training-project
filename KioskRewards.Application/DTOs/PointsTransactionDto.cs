using KioskRewards.Domain.Enums;

namespace KioskRewards.Application.DTOs;

/// <summary>
/// One ledger entry, shaped for the UI (dashboard etc) so we don't have to leak the actual domain
/// entity out of the aggregate.
/// </summary>
public sealed record PointsTransactionDto(
    int Amount,
    TransactionType Type,
    string Description,
    DateTime CreatedUtc);
