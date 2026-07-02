using KioskRewards.Application.Abstractions;
using KioskRewards.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Route-hijacks the memberDashboard page so we can pull the member's balance/history before it renders.
/// Index() has to stay synchronous (Umbraco limitation), so it just blocks on the async version below -
/// that's safe here because ASP.NET Core doesn't have a SynchronizationContext to deadlock on.
/// </summary>
public sealed class MemberDashboardController : RenderController
{
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly ServiceContext _serviceContext;
    private readonly IMemberManager _memberManager;
    private readonly IPointsService _pointsService;

    public MemberDashboardController(
        ILogger<MemberDashboardController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IVariationContextAccessor variationContextAccessor,
        ServiceContext serviceContext,
        IMemberManager memberManager,
        IPointsService pointsService)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _variationContextAccessor = variationContextAccessor;
        _serviceContext = serviceContext;
        _memberManager = memberManager;
        _pointsService = pointsService;
    }

    public override IActionResult Index() => IndexAsync().GetAwaiter().GetResult();

    private async Task<IActionResult> IndexAsync()
    {
        var member = await _memberManager.GetCurrentMemberAsync();
        if (member is null)
        {
            // shouldn't happen, Public Access already keeps anonymous users off this page
            return Unauthorized();
        }

        var balance = await _pointsService.GetBalanceAsync(member.Key);
        var history = await _pointsService.GetHistoryAsync(member.Key);

        var fallback = new PublishedValueFallback(_serviceContext, _variationContextAccessor);
        var model = new MemberDashboardViewModel(CurrentPage!, fallback) { Balance = balance, History = history };

        return CurrentTemplate(model);
    }
}
