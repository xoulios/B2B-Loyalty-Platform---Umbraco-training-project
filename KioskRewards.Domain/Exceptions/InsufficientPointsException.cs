namespace KioskRewards.Domain.Exceptions;

/// <summary>
/// Thrown if someone tries to redeem more points than they actually have. The service layer should
/// already catch this earlier with a Result - this is really just the domain's last line of defence.
/// </summary>
public sealed class InsufficientPointsException : DomainException
{
    public InsufficientPointsException(int balance, int requested)
        : base($"Cannot redeem {requested} points: the balance is only {balance}.")
    {
        Balance = balance;
        Requested = requested;
    }

    public int Balance { get; }
    public int Requested { get; }
}
