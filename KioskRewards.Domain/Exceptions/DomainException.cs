namespace KioskRewards.Domain.Exceptions;

/// <summary>
/// Base for anything that means the domain ended up in a state it should never be in.
/// This is for programming errors, not normal/expected failures - those go through Result instead.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
