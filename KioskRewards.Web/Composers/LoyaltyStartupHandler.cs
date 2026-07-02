using KioskRewards.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace KioskRewards.Web.Composers;

/// <summary>
/// Runs EF migrations + demo seed data once Umbraco has finished starting up. Only runs at
/// RuntimeLevel.Run so it doesn't fire again during install/upgrade restarts.
/// </summary>
public sealed class LoyaltyStartupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<LoyaltyStartupHandler> _logger;

    public LoyaltyStartupHandler(
        IServiceScopeFactory scopeFactory,
        IRuntimeState runtimeState,
        ILogger<LoyaltyStartupHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
            return;

        using var scope = _scopeFactory.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<LoyaltyDataSeeder>();
        await seeder.SeedAsync(cancellationToken);

        _logger.LogInformation("Loyalty database migrated and seeded successfully.");
    }
}
