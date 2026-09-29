using System.Text.Json;
using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Services;

public class GooglePhotosAuthService(
    IDbContextFactory<ChatDbContext> dbContextFactory,
    IOptions<GooglePhotosConfiguration> googlePhotosOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<GooglePhotosAuthService> logger
) : IGooglePhotosAuthService
{
    private const string GoogleOAuthTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GooglePhotosScope =
        "https://www.googleapis.com/auth/photoslibrary.appendonly";

    private readonly GooglePhotosConfiguration _config = googlePhotosOptions.Value;

    public async Task<string> GetAuthorizationUrlAsync(CancellationToken ct)
    {
        var state = Guid.NewGuid().ToString();
        await SaveStateKeyAsync(RootStateKeys.GooglePhotosOAuthState, state, ct);

        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _config.ClientId },
            { "redirect_uri", _config.RedirectUri },
            { "response_type", "code" },
            { "scope", GooglePhotosScope },
            { "state", state },
            { "prompt", "consent" },
            { "access_type", "offline" },
        };

        var query = string.Join(
            "&",
            parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
            )
        );

        return $"{authUrl}?{query}";
    }

    public async Task<GooglePhotosTokens?> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _config.RedirectUri },
            };

            var response = await httpClient.PostAsync(
                GoogleOAuthTokenUrl,
                new FormUrlEncodedContent(requestBody),
                ct
            );

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var tokens = JsonSerializer.Deserialize<GooglePhotosTokens>(responseContent);

                if (tokens is not null)
                {
                    await SaveTokensAsync(tokens, ct);
                    return tokens;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Google OAuth error: {ErrorContent}", errorContent);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обмене кода на токены");
        }

        return null;
    }

    public async Task<bool> IsAuthorizedAsync(CancellationToken ct)
    {
        var token = await GetValidAccessTokenAsync(ct);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct)
    {
        var accessToken = await GetStateValueAsync(RootStateKeys.GooglePhotosAccessToken, ct);
        var expiresAtRaw = await GetStateValueAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            ct
        );

        if (
            !string.IsNullOrWhiteSpace(accessToken)
            && DateTime.TryParse(expiresAtRaw, out var expiresAt)
        )
        {
            expiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);

            if (DateTime.Now < expiresAt)
            {
                return accessToken;
            }
            else
            {
                var newTokens = await RefreshAccessTokenAsync(ct);
                if (newTokens is not null)
                {
                    return newTokens.AccessToken;
                }
            }
        }

        return null;
    }

    private async Task<GooglePhotosTokens?> RefreshAccessTokenAsync(CancellationToken ct)
    {
        var refreshToken = await GetStateValueAsync(RootStateKeys.GooglePhotosRefreshToken, ct);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
            };

            var response = await httpClient.PostAsync(
                GoogleOAuthTokenUrl,
                new FormUrlEncodedContent(requestBody),
                ct
            );

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var tokens = JsonSerializer.Deserialize<GooglePhotosTokens>(responseContent);

                if (tokens is not null)
                {
                    tokens.RefreshToken ??= refreshToken;
                    await SaveTokensAsync(tokens, ct);
                    return tokens;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Google refresh token error: {ErrorContent}", errorContent);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении токена");
        }

        return null;
    }

    private async Task SaveTokensAsync(GooglePhotosTokens tokens, CancellationToken ct)
    {
        var expiresAtUtc = DateTime.Now.AddSeconds(tokens.ExpiresIn - 60);

        await SaveStateKeyAsync(RootStateKeys.GooglePhotosAccessToken, tokens.AccessToken, ct);
        await SaveStateKeyAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            expiresAtUtc.ToString("O"),
            ct
        );
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            await SaveStateKeyAsync(
                RootStateKeys.GooglePhotosRefreshToken,
                tokens.RefreshToken,
                ct
            );
        }
        await SaveStateKeyAsync(RootStateKeys.GooglePhotosIsAuthorized, "true", ct);
    }

    private async Task SaveStateKeyAsync(string key, string value, CancellationToken ct)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);
        var state = await context.RootState.FindAsync(new object[] { key }, cancellationToken: ct);

        if (state is null)
        {
            state = new RootState { Name = key, Value = value };
            context.RootState.Add(state);
        }
        else
        {
            state.Value = value;
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task<string> GetStateValueAsync(string key, CancellationToken ct)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);
        var state = await context
            .RootState.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == key, cancellationToken: ct);
        return state?.Value ?? string.Empty;
    }
}
