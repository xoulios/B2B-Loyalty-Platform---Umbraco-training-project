using KioskRewards.Web.Models;
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
/// Handles login/logout form posts. SurfaceController is Umbraco's way of handling a form submitted
/// from inside a published page - either redirect on success (PRG) or re-render the same page with
/// errors via CurrentUmbracoPage().
/// </summary>
public sealed class MemberAuthController : SurfaceController
{
    private readonly IMemberSignInManager _signInManager;
    private readonly ICultureDictionaryFactory _cultureDictionaryFactory;

    public MemberAuthController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberSignInManager signInManager,
        ICultureDictionaryFactory cultureDictionaryFactory)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _signInManager = signInManager;
        _cultureDictionaryFactory = cultureDictionaryFactory;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]          // anti-CSRF token, comes for free from BeginUmbracoForm
    [ValidateUmbracoFormRouteString]    // checks the ufprt token so the POST lands back on this page
    public async Task<IActionResult> Login(LoginFormModel model)
    {
        if (!ModelState.IsValid)
            return CurrentUmbracoPage();   // re-show the form, tag helpers pull errors from ModelState

        // it's the login username here, not necessarily the email
        var result = await _signInManager.PasswordSignInAsync(
            model.Username, model.Password, isPersistent: model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // send them back wherever they were trying to go before login interrupted them
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToCurrentUmbracoPage();
        }

        // turn the SignInResult into something the user can actually read
        var dictionary = _cultureDictionaryFactory.CreateDictionary();
        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, dictionary["Login.ErrorLockedOut"] ?? "This account is temporarily locked. Please try again later.");
        else if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, dictionary["Login.ErrorNotAllowed"] ?? "This account is not allowed to sign in (check it is approved).");
        else
            ModelState.AddModelError(string.Empty, dictionary["Login.ErrorInvalidCredentials"] ?? "Invalid username or password.");

        return CurrentUmbracoPage();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateUmbracoFormRouteString]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToCurrentUmbracoPage();
    }
}
