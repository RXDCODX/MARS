using MARS.Shared.Models;
using MARS.Telegram.Configuration;
using MARS.Telegram.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GooglePhotosController(
    IGooglePhotosAuthService authService,
    IOptions<GooglePhotosConfiguration> googlePhotosOptions,
    ILogger<GooglePhotosController> logger
) : ControllerBase
{
    private readonly GooglePhotosConfiguration _config = googlePhotosOptions.Value;

    [HttpPost("authorize")]
    public async Task<ActionResult<OperationResult<string>>> AuthorizeAsync(
        CancellationToken ct = default
    )
    {
        if (!_config.Enabled)
        {
            logger.LogWarning("Google Photos disabled");
            return BadRequest(OperationResult<string>.Fail("Disabled"));
        }

        try
        {
            var authUrl = await authService.GetAuthorizationUrlAsync(ct);
            logger.LogInformation("Auth link generated");
            return Ok(OperationResult<string>.Ok(authUrl));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auth error");
            return BadRequest(OperationResult<string>.Fail($"Error: {ex.Message}"));
        }
    }

    [HttpGet("oauth-callback")]
    public async Task<IActionResult> OAuthCallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? error,
        CancellationToken ct = default
    )
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogError("OAuth error: {Error}", error);
            return Ok($"Error: {error}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            logger.LogWarning("No code received");
            return Ok("No code");
        }

        var tokens = await authService.ExchangeCodeForTokenAsync(code, ct);
        if (tokens != null)
        {
            logger.LogInformation("Authorization successful");
            return Ok("Success");
        }

        logger.LogError("Failed to get tokens");
        return Ok("Failed");
    }

    [HttpGet("status")]
    public async Task<ActionResult<OperationResult<object>>> GetStatusAsync(
        CancellationToken ct = default
    )
    {
        var isAuthorized = await authService.IsAuthorizedAsync(ct);
        logger.LogInformation("Status check: {IsAuthorized}", isAuthorized);
        return Ok(OperationResult<object>.Ok(new { isAuthorized }));
    }
}
