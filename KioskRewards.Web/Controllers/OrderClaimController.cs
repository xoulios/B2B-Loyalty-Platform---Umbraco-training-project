using KioskRewards.Application.Abstractions;
using KioskRewards.Web.Composers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Handles the "claim order code" form POST from the member dashboard. This is the one place that
/// bridges Umbraco content (the "CompanyOrder" catalog) and the Umbraco-free loyalty core: it looks
/// up the content node by its Code property, then hands only plain values (Key/PointsValue/a
/// description string) to IOrderClaimService - same division of labor as RewardsController.Redeem.
/// </summary>
public sealed class OrderClaimController : SurfaceController
{
    private readonly IMemberManager _memberManager;
    private readonly IOrderClaimService _orderClaimService;
    private readonly ICultureDictionaryFactory _cultureDictionaryFactory;
    private readonly IPublishedContentQuery _contentQuery;

    public OrderClaimController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberManager memberManager,
        IOrderClaimService orderClaimService,
        ICultureDictionaryFactory cultureDictionaryFactory,
        IPublishedContentQuery contentQuery)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _memberManager = memberManager;
        _orderClaimService = orderClaimService;
        _cultureDictionaryFactory = cultureDictionaryFactory;
        _contentQuery = contentQuery;
    }

    [HttpPost]
    [EnableRateLimiting(OrderClaimRateLimiterComposer.PolicyName)]
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
            TempData["ClaimMessage"] = dictionary.GetValueOrFallback("Dashboard.ClaimCodeRequired", "Please enter an order code.");
            return RedirectToCurrentUmbracoPage();
        }

        // Every published node anywhere in the tree, filtered down to just CompanyOrder ones by
        // type (OfType<T> is plain LINQ - it only keeps items that are actually a CompanyOrder,
        // same idea as "is" pattern matching). Small dataset, so a full-tree scan is plenty fast -
        // no need for Examine search here.
        var normalizedCode = Normalize(code);
        var order = _contentQuery.ContentAtRoot()
            .SelectMany(root => root.Children())
            .SelectMany(node => node.Children())
            .OfType<CompanyOrder>()
            .FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Code) && Normalize(o.Code) == normalizedCode);

        if (order is null)
        {
            TempData["ClaimSuccess"] = false;
            TempData["ClaimMessage"] = "That order code was not found."; // domain-ish message, English on purpose (same as RewardsController)
            return RedirectToCurrentUmbracoPage();
        }

        var result = await _orderClaimService.ClaimAsync(
            member.Key,
            order.Key,
            order.PointsValue,
            $"Order code {order.Code}: {order.ProductDescription}");

        var claimSuccessFormat = dictionary.GetValueOrFallback("Dashboard.ClaimSuccess", "You earned {0} points for: {1}");
        TempData["ClaimSuccess"] = result.IsSuccess;
        TempData["ClaimMessage"] = result.IsSuccess
            ? string.Format(claimSuccessFormat, order.PointsValue, order.ProductDescription)
            : result.Error; // left in English on purpose, it's a domain message not user-facing copy (same as RewardsController)

        return RedirectToCurrentUmbracoPage();
    }

    private static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
