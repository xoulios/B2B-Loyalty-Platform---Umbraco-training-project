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
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Handles the "Redeem" button POST from the rewards catalog. Grabs the reward's title/cost from
/// Umbraco content, then lets IPointsService do the actual points math.
/// </summary>
public sealed class RewardsController : SurfaceController
{
    private readonly IMemberManager _memberManager;
    private readonly IPointsService _pointsService;
    private readonly ICultureDictionaryFactory _cultureDictionaryFactory;

    public RewardsController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberManager memberManager,
        IPointsService pointsService,
        ICultureDictionaryFactory cultureDictionaryFactory)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _memberManager = memberManager;
        _pointsService = pointsService;
        _cultureDictionaryFactory = cultureDictionaryFactory;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]          // anti-CSRF token, comes for free from BeginUmbracoForm
    [ValidateUmbracoFormRouteString]    // checks the ufprt token so the POST lands back on this page
    public async Task<IActionResult> Redeem(Guid rewardKey)
    {
        var member = await _memberManager.GetCurrentMemberAsync();
        if (member is null)
            return Unauthorized(); // shouldn't really happen, Public Access already keeps anonymous users out

        var dictionary = _cultureDictionaryFactory.CreateDictionary();

        var reward = UmbracoContext.Content?.GetById(rewardKey) as Reward;
        if (reward is null)
        {
            TempData["RedeemSuccess"] = false;
            TempData["RedeemMessage"] = dictionary["Rewards.RedeemNotFound"] ?? "That reward could not be found.";
            return RedirectToCurrentUmbracoPage();
        }

        var result = await _pointsService.RedeemAsync(member.Key, reward.PointsCost, $"Redeemed: {reward.Title}");

        var redeemSuccessFormat = dictionary["Rewards.RedeemSuccess"] ?? "You redeemed \"{0}\" for {1} points.";
        TempData["RedeemSuccess"] = result.IsSuccess;
        TempData["RedeemMessage"] = result.IsSuccess
            ? string.Format(redeemSuccessFormat, reward.Title, reward.PointsCost)
            : result.Error; // left in English on purpose, it's a domain message not user-facing copy

        return RedirectToCurrentUmbracoPage();
    }
}
