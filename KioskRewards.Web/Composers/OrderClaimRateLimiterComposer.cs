using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace KioskRewards.Web.Composers;

/// <summary>
/// Rate limits OrderClaimController.ClaimOrder so an authenticated member can't brute-force order
/// codes by repeatedly guessing (plain string match over a small tree scan, no lockout otherwise).
/// Keyed per-member (falls back to IP if somehow unauthenticated) so one member's attempts don't
/// throttle everyone else.
/// </summary>
public sealed class OrderClaimRateLimiterComposer : IComposer
{
    public const string PolicyName = "order-claim";

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "text/plain";
                await context.HttpContext.Response.WriteAsync(
                    "Too many order code attempts. Please wait a minute and try again.",
                    cancellationToken);
            };

            options.AddPolicy(PolicyName, httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.Identity!.Name ?? "unknown-member"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        // PostPipeline, not the more common PostRouting: this must run after Umbraco's own
        // member-cookie UseAuthentication(), otherwise HttpContext.User above is still anonymous
        // and the per-member partition key silently degrades to the IP fallback.
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(nameof(OrderClaimRateLimiterComposer))
            {
                PostPipeline = app => app.UseRateLimiter()
            });
        });
    }
}
