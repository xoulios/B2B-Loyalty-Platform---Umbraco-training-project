namespace KioskRewards.Application.Configuration;

/// <summary>
/// Loyalty-program tunables that are safe to change without a code deploy (bound from
/// appsettings.json under the "Loyalty" section).
/// </summary>
public sealed class LoyaltyOptions
{
    public const string SectionName = "Loyalty";

    /// Points a new kiosk-owner member gets on their very first save.
    public int WelcomeBonusPoints { get; set; } = 100;
}
