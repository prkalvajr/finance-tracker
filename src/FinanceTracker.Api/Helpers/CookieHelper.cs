using FinanceTracker.Application.DTOs.Auth;

namespace FinanceTracker.Api.Helpers;

public static class CookieHelper
{
    public const string AccessTokenCookieName = "access_token";
    public const string RefreshTokenCookieName = "refresh_token";

    public static void SetTokenCookies(HttpResponse response, AuthTokens tokens)
    {
        response.Cookies.Append(AccessTokenCookieName, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.AccessTokenExpiry,
            Path = "/",
        });

        response.Cookies.Append(RefreshTokenCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.RefreshTokenExpiry,
            Path = "/",
        });
    }

    public static void ClearTokenCookies(HttpResponse response)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        };

        response.Cookies.Delete(AccessTokenCookieName, options);
        response.Cookies.Delete(RefreshTokenCookieName, options);
    }
}
