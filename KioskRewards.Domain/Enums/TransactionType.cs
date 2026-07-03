namespace KioskRewards.Domain.Enums;

/// Direction of a transaction - did it add points or spend them?
public enum TransactionType
{
    /// Points came in
    Earn = 1,

    /// Points went out
    Redeem = 2
}
