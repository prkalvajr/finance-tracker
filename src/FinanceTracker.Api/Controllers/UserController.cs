using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("/")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("user")]
    public async Task<ActionResult<UserDto>> GetUser(CancellationToken ct)
    {
        var userId = GetUserId();
        var dto = await _userService.GetCurrentUserAsync(userId, ct);
        return Ok(dto);
    }

    [HttpPut("update")]
    public async Task<ActionResult<UserDto>> Update([FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var dto = await _userService.UpdateUserAsync(userId, request, ct);
        return Ok(dto);
    }

    private int GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (sub is null || !int.TryParse(sub, out var id))
        {
            throw new UnauthorizedException("User identity not found.");
        }

        return id;
    }
}
