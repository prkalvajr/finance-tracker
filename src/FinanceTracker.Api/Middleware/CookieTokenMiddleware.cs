using FinanceTracker.Api.Helpers;

namespace FinanceTracker.Api.Middleware;

public class CookieTokenMiddleware
{
    private readonly RequestDelegate _next;

    public CookieTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization") &&
            context.Request.Cookies.TryGetValue(CookieHelper.AccessTokenCookieName, out var accessToken) &&
            !string.IsNullOrWhiteSpace(accessToken))
        {
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
        }

        await _next(context);
    }
}
