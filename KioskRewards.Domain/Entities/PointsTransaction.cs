using KioskRewards.Domain.Enums;

namespace KioskRewards.Domain.Entities;

/// <summary>
/// One immutable entry in a member's points ledger
/// </summary>
public class PointsTransaction
{
    // EF Core materialisation constructor. private keeps construction inside the domain.
    private PointsTransaction() { }

    internal PointsTransaction(Guid memberKey, int amount, TransactionType type, string description, DateTime createdUtc)
    {
        MemberKey = memberKey;
        Amount = amount;
        Type = type;
        Description = description;
        CreatedUtc = createdUtc;
    }

    public int Id { get; private set; }

    /// The Umbraco Member this entry belongs to FK PointsAccount
    public Guid MemberKey { get; private set; }

    public int Amount { get; private set; }

    public TransactionType Type { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime CreatedUtc { get; private set; }
}
