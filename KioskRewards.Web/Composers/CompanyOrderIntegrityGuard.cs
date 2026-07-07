using KioskRewards.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace KioskRewards.Web.Composers;

/// <summary>
/// Blocks deleting or unpublishing a "CompanyOrder" node once its order code has already been
/// claimed. The EF OrderCode table only stores a reference to the content Key (see
/// KioskRewards.Domain.Entities.OrderCode) - if the node disappeared, that reference would go
/// dangling and the ledger entry it produced would point at nothing. Same
/// INotificationAsyncHandler idiom as MemberSavedLoyaltyHandler, just cancelable here.
/// </summary>
public sealed class CompanyOrderIntegrityGuard :
    INotificationAsyncHandler<ContentDeletingNotification>,
    INotificationAsyncHandler<ContentUnpublishingNotification>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CompanyOrderIntegrityGuard(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task HandleAsync(ContentDeletingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var content in notification.DeletedEntities)
        {
            if (!IsCompanyOrder(content) || !await IsClaimedAsync(content.Key, cancellationToken))
                continue;

            notification.CancelOperation(new EventMessage(
                "Order code protected",
                $"'{content.Name}' has already been claimed by a member and can't be deleted - it would leave a dangling reference in the points ledger.",
                EventMessageType.Error));
            return; // one blocked item is enough to reject the whole batch
        }
    }

    public async Task HandleAsync(ContentUnpublishingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var content in notification.UnpublishedEntities)
        {
            if (!IsCompanyOrder(content) || !await IsClaimedAsync(content.Key, cancellationToken))
                continue;

            notification.CancelOperation(new EventMessage(
                "Order code protected",
                $"'{content.Name}' has already been claimed by a member and can't be unpublished.",
                EventMessageType.Error));
            return;
        }
    }

    private static bool IsCompanyOrder(IContent content) =>
        string.Equals(content.ContentType.Alias, CompanyOrder.ModelTypeAlias, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsClaimedAsync(Guid contentKey, CancellationToken ct)
    {
        // INotificationAsyncHandler is registered once and reused across requests, so it can't just
        // hold a scoped IOrderClaimService in a constructor field - it needs a fresh DI scope per call.
        using var scope = _scopeFactory.CreateScope();
        var orderClaimService = scope.ServiceProvider.GetRequiredService<IOrderClaimService>();
        return await orderClaimService.IsClaimedAsync(contentKey, ct);
    }
}
