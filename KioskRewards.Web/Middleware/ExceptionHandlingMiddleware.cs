using KioskRewards.Domain.Exceptions;

namespace KioskRewards.Web.Middleware;

/// <summary>
/// Last-resort handler: logs anything that escaped the controllers/services and shows the
/// friendly error page instead of the default Umbraco/ASP.NET one. Domain-invariant violations
/// are logged as warnings (they signal a coding/orchestration gap, since expected failures are
/// returned as Results - see KioskRewards.Domain.Common.Result); everything else is an error.
/// Only registered outside Development (see ExceptionHandlingComposer) so local debugging still
/// gets Umbraco's own detailed error output.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string ErrorPath = "/error.html";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation handling {Method} {Path}",
                context.Request.Method, context.Request.Path);
            Redirect(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception handling {Method} {Path}",
                context.Request.Method, context.Request.Path);
            Redirect(context);
        }
    }

    private static void Redirect(HttpContext context)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.Redirect(ErrorPath);
    }
}
