using KioskRewards.Web.Middleware;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace KioskRewards.Web.Composers;

/// <summary>
/// Registers ExceptionHandlingMiddleware as early as possible (PrePipeline), so it wraps every
/// other Umbraco/ASP.NET middleware and can catch whatever escapes them.
/// </summary>
public sealed class ExceptionHandlingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(nameof(ExceptionHandlingComposer))
            {
                PrePipeline = app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
                    if (!env.IsDevelopment())
                        app.UseMiddleware<ExceptionHandlingMiddleware>();
                }
            });
        });
    }
}
