using KioskRewards.Application.Configuration;
using KioskRewards.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace KioskRewards.Web.Composers;

/// <summary>
/// This is basically our Program.cs DI wiring, Umbraco-style. Composers get picked up automatically
/// by .AddComposers(), so there's nothing to register by hand.
/// </summary>
public sealed class LoyaltyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<LoyaltyOptions>(builder.Config.GetSection(LoyaltyOptions.SectionName));

        var rawConnectionString = builder.Config.GetConnectionString("LoyaltyDb")
            ?? "Data Source=|DataDirectory|/loyalty.sqlite.db;Cache=Shared;Foreign Keys=True;Pooling=True";

        builder.Services.AddLoyaltyInfrastructure(sp =>
        {
            // have to resolve |DataDirectory| ourselves - Microsoft.Data.Sqlite doesn't expand it
            // like Umbraco does for its own connection string
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dataDirectory = Path.Combine(env.ContentRootPath, "umbraco", "Data");
            Directory.CreateDirectory(dataDirectory);
            return rawConnectionString.Replace("|DataDirectory|", dataDirectory);
        });

        // runs migrations + seeds demo data once the app is up
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, LoyaltyStartupHandler>();

        // gives new kiosk-owner members a loyalty account + welcome bonus automatically
        builder.AddNotificationAsyncHandler<MemberSavedNotification, MemberSavedLoyaltyHandler>();

        // blocks delete/unpublish of a CompanyOrder node that's already been claimed
        builder.AddNotificationAsyncHandler<ContentDeletingNotification, CompanyOrderIntegrityGuard>();
        builder.AddNotificationAsyncHandler<ContentUnpublishingNotification, CompanyOrderIntegrityGuard>();
    }
}
