using MARS.Shared.Models;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using Microsoft.AspNetCore.Mvc;

namespace MARS.SoundRequest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpotifyAuthController(SpotifyAuthService spotifyAuthService) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<OperationResult<SpotifyAuthStartResult>>> StartAuthorization(
        [FromBody] SpotifyAuthStartRequest? request,
        CancellationToken ct
    )
    {
        var result = OperationResult<SpotifyAuthStartResult>.Fail(
            "Не удалось подготовить авторизацию Spotify"
        );

        if (
            request is not null
            && !string.IsNullOrWhiteSpace(request.ClientId)
            && !string.IsNullOrWhiteSpace(request.ClientSecret)
        )
        {
            var redirectUri = ResolveRedirectUri(request.RedirectUri);
            var authResult = await spotifyAuthService.StartAuthorizationAsync(
                request.ClientId,
                request.ClientSecret,
                redirectUri,
                ct
            );

            if (authResult.Success)
            {
                result = OperationResult<SpotifyAuthStartResult>.Ok(authResult);
            }
            else
            {
                result = OperationResult<SpotifyAuthStartResult>.Fail(authResult.Message);
            }
        }
        else
        {
            result = OperationResult<SpotifyAuthStartResult>.Fail("Нужны ClientId и ClientSecret");
        }

        return result;
    }

    [HttpGet("callback")]
    public async Task<ActionResult<OperationResult<SpotifyAuthCompleteResult>>> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? redirectUri,
        CancellationToken ct
    )
    {
        var result = OperationResult<SpotifyAuthCompleteResult>.Fail(
            "Не удалось завершить авторизацию Spotify"
        );

        if (!string.IsNullOrWhiteSpace(error))
        {
            var message = string.IsNullOrWhiteSpace(errorDescription)
                ? $"Spotify OAuth ошибка: {error}"
                : $"Spotify OAuth ошибка: {error} ({errorDescription})";
            result = OperationResult<SpotifyAuthCompleteResult>.Fail(message);
        }
        else if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(state))
        {
            var callbackRedirectUri = ResolveRedirectUri(redirectUri);
            var authResult = await spotifyAuthService.CompleteAuthorizationAsync(
                code,
                state,
                callbackRedirectUri,
                ct
            );

            if (authResult.Success)
            {
                result = OperationResult<SpotifyAuthCompleteResult>.Ok(authResult);
            }
            else
            {
                result = OperationResult<SpotifyAuthCompleteResult>.Fail(authResult.Message);
            }
        }
        else
        {
            result = OperationResult<SpotifyAuthCompleteResult>.Fail("Code и state обязательны");
        }

        return result;
    }

    [HttpGet("status")]
    public async Task<ActionResult<OperationResult<SpotifyAuthStatusResult>>> GetStatus(
        CancellationToken ct
    )
    {
        var status = await spotifyAuthService.GetStatusAsync(ct);
        var result = status.IsLinked
            ? OperationResult<SpotifyAuthStatusResult>.Ok(status)
            : OperationResult<SpotifyAuthStatusResult>.Fail(status.Message);

        return result;
    }

    [HttpPost("disconnect")]
    public async Task<ActionResult<OperationResult>> Disconnect(CancellationToken ct)
    {
        var disconnected = await spotifyAuthService.DisconnectAsync(ct);
        var result = disconnected
            ? OperationResult.Ok()
            : OperationResult.Fail("Не удалось отключить Spotify аккаунт");

        return result;
    }

    private string ResolveRedirectUri(string? redirectUri)
    {
        var result = redirectUri;

        if (string.IsNullOrWhiteSpace(result))
        {
            result = $"{Request.Scheme}://{Request.Host}/api/SpotifyAuth/callback";
        }

        return result;
    }
}
