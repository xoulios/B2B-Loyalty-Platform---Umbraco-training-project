using KioskRewards.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Handles the "claim order code" form POST from the member dashboard. Same shape as RewardsController -
/// just hands the code off to IOrderClaimService and turns the Result into a TempData banner.
/// </summary>
public sealed class OrderClaimController : SurfaceController
{
    private readonly IMemberManager _memberManager;
    private readonly IOrderClaimService _orderClaimService;
    private readonly ICultureDictionaryFactory _cultureDictionaryFactory;

    public OrderClaimController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberManager memberManager,
        IOrderClaimService orderClaimService,
        ICultureDictionaryFactory cultureDictionaryFactory)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _memberManager = memberManager;
        _orderClaimService = orderClaimService;
        _cultureDictionaryFactory = cultureDictionaryFactory;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]          // anti-CSRF token, comes for free from BeginUmbracoForm
    [ValidateUmbracoFormRouteString]    // checks the ufprt token so the POST lands back on this page
    public async Task<IActionResult> ClaimOrder(string code)
    {
        var member = await _memberManager.GetCurrentMemberAsync();
        if (member is null)
            return Unauthorized(); // shouldn't really happen, Public Access already keeps anonymous users out

        var dictionary = _cultureDictionaryFactory.CreateDictionary();

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ClaimSuccess"] = false;
            TempData["ClaimMessage"] = dictionary["Dashboard.ClaimCodeRequired"] ?? "Please enter an order code.";
            return RedirectToCurrentUmbracoPage();
        }

        var result = await _orderClaimService.ClaimAsync(member.Key, code);

        var claimSuccessFormat = dictionary["Dashboard.ClaimSuccess"] ?? "You earned {0} points for: {1}";
        TempData["ClaimSuccess"] = result.IsSuccess;
        TempData["ClaimMessage"] = result.IsSuccess
            ? string.Format(claimSuccessFormat, result.Value.PointsAwarded, result.Value.ProductDescription)
            : result.Error; // left in English on purpose, it's a domain message not user-facing copy (same as RewardsController)

        return RedirectToCurrentUmbracoPage();
    }
}
