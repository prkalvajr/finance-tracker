using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("/")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("transaction")]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var dto = await _transactionService.CreateAsync(userId, request, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("transaction")]
    public async Task<ActionResult<TransactionDto>> Update([FromBody] UpdateTransactionRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var dto = await _transactionService.UpdateAsync(userId, request, ct);
        return Ok(dto);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetPaged([FromQuery] TransactionQueryParams query, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _transactionService.GetPagedAsync(userId, query, ct);
        return Ok(result);
    }

    [HttpDelete("transaction")]
    public async Task<IActionResult> Delete([FromQuery] int id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _transactionService.SoftDeleteAsync(userId, id, ct);
        return NoContent();
    }

    [HttpGet("transactions/summary")]
    public async Task<ActionResult<TransactionSummaryDto>> GetSummary(CancellationToken ct)
    {
        var userId = GetUserId();
        var summary = await _transactionService.GetSummaryAsync(userId, ct);
        return Ok(summary);
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
