namespace KioskRewards.Domain.Enums;

/// Direction of a transaction - did it add points or spend them?
public enum TransactionType
{
    /// Points came in, e.g. from a sale
    Earn = 1,

    /// Points went out, spent on a reward
    Redeem = 2
}
