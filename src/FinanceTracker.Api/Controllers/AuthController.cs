using FinanceTracker.Api.Helpers;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("/")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var tokens = await _authService.RegisterAsync(request, ct);
        CookieHelper.SetTokenCookies(Response, tokens);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var tokens = await _authService.LoginAsync(request, ct);
        CookieHelper.SetTokenCookies(Response, tokens);
        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(CookieHelper.RefreshTokenCookieName, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Missing refresh token.");
        }

        var tokens = await _authService.RefreshAsync(refreshToken, ct);
        CookieHelper.SetTokenCookies(Response, tokens);
        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(CookieHelper.RefreshTokenCookieName, out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, ct);
        }

        CookieHelper.ClearTokenCookies(Response);
        return Ok();
    }
}
