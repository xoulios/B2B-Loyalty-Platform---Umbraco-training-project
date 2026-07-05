namespace KioskRewards.Domain.Exceptions;

/// <summary>
/// Thrown if someone tries to claim an order code a second time. The service layer should already
/// catch this earlier with a Result - this is really just the domain's last line of defence.
/// </summary>
public sealed class OrderCodeAlreadyClaimedException : DomainException
{
    public OrderCodeAlreadyClaimedException(string code)
        : base($"Order code '{code}' has already been claimed.")
    {
        Code = code;
    }

    public string Code { get; }
}
