using LinkLab.BuildingBlocks.Core.Primitives;
using LinkLab.BuildingBlocks.Idempotency;
using LinkLab.Identity.Api.Core.DTOs;
using LinkLab.Identity.Api.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinkLab.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [Idempotent("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var handle = HttpContext.GetIdempotencyHandle();

        var result = await authService.RegisterAsync(request, handle, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return CreatedAtAction(nameof(Register), new { id = result.Value.UserId }, result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        var result = await authService.LoginAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        var result = await authService.RefreshTokenAsync(request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }
}
