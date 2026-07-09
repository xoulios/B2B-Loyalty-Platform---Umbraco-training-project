using KioskRewards.Application.Abstractions;
using KioskRewards.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace KioskRewards.Web.Composers;

/// <summary>
/// Makes sure every kiosk-owner member has a loyalty account. This fires on every save (create AND
/// edit), so we only hand out the welcome bonus when they don't have any history yet - otherwise
/// every edit would trigger another bonus.
/// </summary>
public sealed class MemberSavedLoyaltyHandler : INotificationAsyncHandler<MemberSavedNotification>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemberSavedLoyaltyHandler> _logger;
    private readonly int _welcomeBonus;

    public MemberSavedLoyaltyHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<MemberSavedLoyaltyHandler> logger,
        IOptions<LoyaltyOptions> loyaltyOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _welcomeBonus = loyaltyOptions.Value.WelcomeBonusPoints;
    }

    public async Task HandleAsync(MemberSavedNotification notification, CancellationToken cancellationToken)
    {
        // IPointsService is scoped (owns a DbContext), so we need our own DI scope to grab it here
        using var scope = _scopeFactory.CreateScope();
        var points = scope.ServiceProvider.GetRequiredService<IPointsService>();

        foreach (var member in notification.SavedEntities)
        {
            // skip anyone that isn't a kiosk owner
            if (!string.Equals(member.ContentType.Alias, KioskOwner.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
                continue;

            var history = await points.GetHistoryAsync(member.Key, cancellationToken);
            if (history.Count > 0)
                continue;   // already has an account, nothing to do here

            var result = await points.EarnAsync(member.Key, _welcomeBonus, "Welcome bonus", cancellationToken);
            if (result.IsSuccess)
                _logger.LogInformation("Provisioned loyalty account for member {MemberKey} with {Points} welcome points.", member.Key, _welcomeBonus);
            else
                _logger.LogWarning("Failed to provision loyalty account for member {MemberKey}: {Error}", member.Key, result.Error);
        }
    }
}
