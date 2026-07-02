using KioskRewards.Domain.Enums;
using KioskRewards.Domain.Exceptions;

namespace KioskRewards.Domain.Entities;

/// <summary>
/// Aggregate root for a single kiosk owner's loyalty points. It owns the transaction ledger and
/// guards the core invariant: the balance can never go negative
/// </summary>
public class PointsAccount
{
    private readonly List<PointsTransaction> _transactions = new();

    // EF Core materialisation constructor.
    private PointsAccount() { }

    private PointsAccount(Guid memberKey)
    {
        MemberKey = memberKey;
        Balance = 0;
    }

    /// The Umbraco Member key this account belongs to. PK for PointsAccount, FK for PointsTransaction
    public Guid MemberKey { get; private set; }

    public int Balance { get; private set; }

    /// Optimistic-concurrency token: guards against two redemptions racing on the same balance
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    /// Read-only view of the ledger; callers cannot mutate it directly
    public IReadOnlyCollection<PointsTransaction> Transactions => _transactions.AsReadOnly();
    public static PointsAccount Create(Guid memberKey)
    {
        if (memberKey == Guid.Empty)
            throw new ArgumentException("Member key is required.", nameof(memberKey));

        return new PointsAccount(memberKey);
    }
    public bool CanRedeem(int cost) => cost > 0 && cost <= Balance;

    public PointsTransaction Earn(int amount, string description, DateTime nowUtc)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Earned points must be positive.");

        var tx = new PointsTransaction(MemberKey, amount, TransactionType.Earn, description, nowUtc);
        _transactions.Add(tx);
        Balance += amount;
        return tx;
    }
    public PointsTransaction Redeem(int cost, string description, DateTime nowUtc)
    {
        if (cost <= 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Redeem cost must be positive.");
        if (!CanRedeem(cost))
            throw new InsufficientPointsException(Balance, cost);

        var tx = new PointsTransaction(MemberKey, cost, TransactionType.Redeem, description, nowUtc);
        _transactions.Add(tx);
        Balance -= cost;
        return tx;
    }
}
