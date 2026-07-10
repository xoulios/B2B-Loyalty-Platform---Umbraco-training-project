using KioskRewards.Domain.Exceptions;
using KioskRewards.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KioskRewards.Tests.Web;

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateSut(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static ExceptionHandlingMiddleware CreateSut(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) =>
        new(next, logger);

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

    [Fact]
    public async Task InvokeAsync_UnhandledException_StripsNewlinesFromLoggedRequestPath()
    {
        var capturingLogger = new CapturingLogger<ExceptionHandlingMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Path = "/legit\r\nFORGED LOG LINE: admin login succeeded";
        var sut = CreateSut(_ => throw new InvalidOperationException("boom"), capturingLogger);

        await sut.InvokeAsync(context);

        Assert.DoesNotContain('\r', capturingLogger.LastMessage);
        Assert.DoesNotContain('\n', capturingLogger.LastMessage);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public string LastMessage { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LastMessage = formatter(state, exception);
        }
    }
}
