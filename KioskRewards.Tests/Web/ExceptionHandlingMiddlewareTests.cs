using KioskRewards.Domain.Exceptions;
using KioskRewards.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace KioskRewards.Tests.Web;

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateSut(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    [Fact]
    public async Task InvokeAsync_NoException_LeavesResponseUntouched()
    {
        var context = new DefaultHttpContext();
        var sut = CreateSut(_ => Task.CompletedTask);

        await sut.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_DomainException_RedirectsToErrorPageInsteadOfThrowing()
    {
        var context = new DefaultHttpContext();
        var sut = CreateSut(_ => throw new InsufficientPointsException(balance: 10, requested: 20));

        await sut.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/error.html", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_RedirectsToErrorPageInsteadOfThrowing()
    {
        var context = new DefaultHttpContext();
        var sut = CreateSut(_ => throw new InvalidOperationException("boom"));

        await sut.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/error.html", context.Response.Headers.Location);
    }
}
