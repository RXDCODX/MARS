using System.Globalization;
using System.Text.Json.Serialization;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.SoundRequest.Services;

public class SpotifyAuthService(
    IDbContextFactory<MediaDbContext> dbContextFactory,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<SpotifyAuthService> logger
)
{
    private const string SpotifyHttpClientName = "spotify-auth";

    private const string SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";
    private const string SpotifyTokenUrl = "https://accounts.spotify.com/api/token";
    private const string SpotifyMeUrl = "https://api.spotify.com/v1/me";

    /// <summary>
    /// Аудит: сервис создавал <c>new HttpClient()</c> на каждый вызов, что под
    /// нагрузкой исчерпывало сокеты. Берём клиент у фабрики — она переиспользует
    /// обработчик сообщений и утилизирует сокеты только при смене handler.
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient(SpotifyHttpClientName);
        client.Timeout = TimeSpan.FromSeconds(20);
        return client;
    }

    private static readonly string[] Scopes =
    [
        "user-read-private",
        "user-read-email",
        "user-read-playback-state",
        "user-modify-playback-state",
    ];

    public async Task<SpotifyAuthCredentials> GetCredentialsAsync(CancellationToken ct)
    {
        var clientId = await GetRootStateValueAsync(RootStateKeys.SoundRequestSpotifyClientId, ct);
        var clientSecret = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyClientSecret,
            ct
        );
        var refreshToken = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyRefreshToken,
            ct
        );
        var accessToken = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyAccessToken,
            ct
        );
        var expiresAtRaw = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyAccessTokenExpiresAtUtc,
            ct
        );
        var deviceIdFromState = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyDeviceId,
            ct
        );

        // Fallback to configuration
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = configuration["Spotify:ClientId"] ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            clientSecret = configuration["Spotify:ClientSecret"] ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            refreshToken = configuration["Spotify:RefreshToken"] ?? string.Empty;
        }

        // Пункт №12 аудита: значение читается как UTC, а сравнивалось с DateTime.Now.
        // На хосте восточнее UTC токен считался валидным на величину локального offset
        // и давал лишние 401. Здесь нормализуем и запись, и чтение к UTC.
        var expiresAtUtc = DateTime.UnixEpoch;
        if (
            !string.IsNullOrWhiteSpace(expiresAtRaw)
            && DateTimeOffset.TryParse(
                expiresAtRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedExpiresAt
            )
        )
        {
            expiresAtUtc = parsedExpiresAt.UtcDateTime;
        }

        return new SpotifyAuthCredentials
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshToken = refreshToken,
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = expiresAtUtc,
            DeviceId = string.IsNullOrWhiteSpace(deviceIdFromState)
                ? configuration["Spotify:DeviceId"] ?? string.Empty
                : deviceIdFromState,
        };
    }

    public async Task<SpotifyAuthStartResult> StartAuthorizationAsync(
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken ct
    )
    {
        var result = new SpotifyAuthStartResult
        {
            Success = false,
            Message = "Не удалось подготовить авторизацию Spotify",
        };

        if (
            !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            && !string.IsNullOrWhiteSpace(redirectUri)
        )
        {
            var state = Guid.NewGuid().ToString("N");
            var normalizedRedirectUri = redirectUri.Trim();

            await UpsertRootStateAsync(
                RootStateKeys.SoundRequestSpotifyClientId,
                clientId.Trim(),
                ct
            );
            await UpsertRootStateAsync(
                RootStateKeys.SoundRequestSpotifyClientSecret,
                clientSecret.Trim(),
                ct
            );
            await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyOAuthState, state, ct);
            await UpsertRootStateAsync(
                RootStateKeys.SoundRequestSpotifyRedirectUri,
                normalizedRedirectUri,
                ct
            );

            var scope = string.Join(' ', Scopes);
            var authUrl =
                $"{SpotifyAuthorizeUrl}?response_type=code"
                + $"&client_id={Uri.EscapeDataString(clientId.Trim())}"
                + $"&scope={Uri.EscapeDataString(scope)}"
                + $"&state={Uri.EscapeDataString(state)}"
                + "&show_dialog=true"
                + $"&redirect_uri={normalizedRedirectUri}";

            result = new SpotifyAuthStartResult
            {
                Success = true,
                Message = "Ссылка на авторизацию Spotify подготовлена",
                AuthUrl = authUrl,
                State = state,
            };
        }
        else
        {
            result = new SpotifyAuthStartResult
            {
                Success = false,
                Message = "ClientId, ClientSecret и redirectUri обязательны",
            };
        }

        return result;
    }

    public async Task<SpotifyAuthCompleteResult> CompleteAuthorizationAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken ct
    )
    {
        var result = new SpotifyAuthCompleteResult
        {
            Success = false,
            Message = "Не удалось завершить авторизацию Spotify",
        };

        if (
            !string.IsNullOrWhiteSpace(code)
            && !string.IsNullOrWhiteSpace(state)
            && !string.IsNullOrWhiteSpace(redirectUri)
        )
        {
            var storedState = await GetRootStateValueAsync(
                RootStateKeys.SoundRequestSpotifyOAuthState,
                ct
            );

            if (string.Equals(storedState, state, StringComparison.Ordinal))
            {
                var credentials = await GetCredentialsAsync(ct);

                if (
                    !string.IsNullOrWhiteSpace(credentials.ClientId)
                    && !string.IsNullOrWhiteSpace(credentials.ClientSecret)
                )
                {
                    try
                    {
                        var storedRedirect = await GetRootStateValueAsync(
                            RootStateKeys.SoundRequestSpotifyRedirectUri,
                            ct
                        );

                        using var httpClient = CreateHttpClient();
                        var requestBody = new Dictionary<string, string>
                        {
                            { "grant_type", "authorization_code" },
                            { "code", code },
                            { "redirect_uri", string.IsNullOrWhiteSpace(storedRedirect) ? redirectUri : storedRedirect },
                            { "client_id", credentials.ClientId },
                            { "client_secret", credentials.ClientSecret },
                        };

                        var response = await httpClient.PostAsync(
                            SpotifyTokenUrl,
                            new FormUrlEncodedContent(requestBody),
                            ct
                        );

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync(ct);
                            var token = System.Text.Json.JsonSerializer.Deserialize<SpotifyTokenResponseDto>(content);

                            if (!string.IsNullOrWhiteSpace(token?.AccessToken))
                            {
                                await PersistTokenStateAsync(token, ct);

                                var profile = await GetProfileAsync(token.AccessToken, ct);
                                await PersistProfileAsync(profile, ct);

                                result = new SpotifyAuthCompleteResult
                                {
                                    Success = true,
                                    Message = "Spotify аккаунт подключен",
                                    DisplayName = profile?.DisplayName,
                                    Product = profile?.Product,
                                };
                            }
                            else
                            {
                                result = new SpotifyAuthCompleteResult
                                {
                                    Success = false,
                                    Message = "Spotify не вернул access token",
                                };
                            }
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync(ct);
                            logger.LogError("Spotify token exchange error: {Error}", errorContent);
                            result = new SpotifyAuthCompleteResult
                            {
                                Success = false,
                                Message = "Ошибка обмена OAuth-кода на токен Spotify",
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка завершения Spotify OAuth");
                        result = new SpotifyAuthCompleteResult
                        {
                            Success = false,
                            Message = "Ошибка обмена OAuth-кода на токен Spotify",
                        };
                    }
                }
                else
                {
                    result = new SpotifyAuthCompleteResult
                    {
                        Success = false,
                        Message = "Не найдены ClientId/ClientSecret для Spotify",
                    };
                }
            }
            else
            {
                result = new SpotifyAuthCompleteResult
                {
                    Success = false,
                    Message = "Проверка OAuth state не пройдена",
                };
            }
        }
        else
        {
            result = new SpotifyAuthCompleteResult
            {
                Success = false,
                Message = "Code, state и redirectUri обязательны",
            };
        }

        return result;
    }

    public async Task<SpotifyAuthStatusResult> GetStatusAsync(CancellationToken ct)
    {
        var credentials = await GetCredentialsAsync(ct);
        var displayName = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyDisplayName,
            ct
        );
        var userId = await GetRootStateValueAsync(RootStateKeys.SoundRequestSpotifyUserId, ct);
        var avatarUrl = await GetRootStateValueAsync(
            RootStateKeys.SoundRequestSpotifyAvatarUrl,
            ct
        );
        var product = await GetRootStateValueAsync(RootStateKeys.SoundRequestSpotifyProduct, ct);

        var hasClientCredentials =
            !string.IsNullOrWhiteSpace(credentials.ClientId)
            && !string.IsNullOrWhiteSpace(credentials.ClientSecret);
        var isLinked = hasClientCredentials && !string.IsNullOrWhiteSpace(credentials.RefreshToken);

        return new SpotifyAuthStatusResult
        {
            IsLinked = isLinked,
            HasClientCredentials = hasClientCredentials,
            DisplayName = displayName,
            UserId = userId,
            AvatarUrl = avatarUrl,
            Product = product,
            DeviceId = credentials.DeviceId,
            AccessTokenExpiresAtUtc =
                credentials.AccessTokenExpiresAtUtc > DateTime.UnixEpoch
                    ? credentials.AccessTokenExpiresAtUtc
                    : null,
            Message = isLinked ? "Spotify подключен" : "Spotify не подключен",
        };
    }

    public async Task<bool> DisconnectAsync(CancellationToken ct)
    {
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyRefreshToken, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyAccessToken, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyAccessTokenExpiresAtUtc, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyDisplayName, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyUserId, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyAvatarUrl, string.Empty, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyProduct, string.Empty, ct);

        return true;
    }

    public async Task<SpotifyAccessTokenResult> GetValidAccessTokenAsync(CancellationToken ct)
    {
        var result = new SpotifyAccessTokenResult
        {
            Success = false,
            Message = "Не удалось получить Spotify access token",
        };

        var credentials = await GetCredentialsAsync(ct);

        if (
            !string.IsNullOrWhiteSpace(credentials.ClientId)
            && !string.IsNullOrWhiteSpace(credentials.ClientSecret)
            && !string.IsNullOrWhiteSpace(credentials.RefreshToken)
        )
        {
            if (
                !string.IsNullOrWhiteSpace(credentials.AccessToken)
                && credentials.AccessTokenExpiresAtUtc > DateTime.UtcNow.AddSeconds(30)
            )
            {
                result = new SpotifyAccessTokenResult
                {
                    Success = true,
                    Message = "Используется сохраненный Spotify access token",
                    AccessToken = credentials.AccessToken,
                    ExpiresAtUtc = credentials.AccessTokenExpiresAtUtc,
                    DeviceId = credentials.DeviceId,
                };
            }
            else
            {
                try
                {
                    using var httpClient = CreateHttpClient();
                    var requestBody = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", credentials.RefreshToken },
                        { "client_id", credentials.ClientId },
                        { "client_secret", credentials.ClientSecret },
                    };

                    var response = await httpClient.PostAsync(
                        SpotifyTokenUrl,
                        new FormUrlEncodedContent(requestBody),
                        ct
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(ct);
                        var token = System.Text.Json.JsonSerializer.Deserialize<SpotifyTokenResponseDto>(content);

                        if (!string.IsNullOrWhiteSpace(token?.AccessToken))
                        {
                            await PersistTokenStateAsync(token, ct);
                            var updated = await GetCredentialsAsync(ct);

                            result = new SpotifyAccessTokenResult
                            {
                                Success = true,
                                Message = "Spotify access token обновлен",
                                AccessToken = updated.AccessToken,
                                ExpiresAtUtc = updated.AccessTokenExpiresAtUtc,
                                DeviceId = updated.DeviceId,
                            };
                        }
                        else
                        {
                            result = new SpotifyAccessTokenResult
                            {
                                Success = false,
                                Message = "Spotify не вернул access token при refresh",
                            };
                        }
                    }
                    else
                    {
                        result = new SpotifyAccessTokenResult
                        {
                            Success = false,
                            Message = "Ошибка HTTP при обновлении Spotify токена",
                        };
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка обновления Spotify access token");
                    result = new SpotifyAccessTokenResult
                    {
                        Success = false,
                        Message = "Ошибка обновления Spotify токена",
                    };
                }
            }
        }
        else
        {
            result = new SpotifyAccessTokenResult
            {
                Success = false,
                Message = "Spotify не настроен: нужны ClientId, ClientSecret и RefreshToken",
            };
        }

        return result;
    }

    public async Task<bool> SaveDeviceIdAsync(string? deviceId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyDeviceId, deviceId, ct);
            return true;
        }
        return false;
    }

    private async Task PersistTokenStateAsync(SpotifyTokenResponseDto token, CancellationToken ct)
    {
        var expiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 30));

        await UpsertRootStateAsync(
            RootStateKeys.SoundRequestSpotifyAccessToken,
            token.AccessToken ?? string.Empty,
            ct
        );

        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            await UpsertRootStateAsync(
                RootStateKeys.SoundRequestSpotifyRefreshToken,
                token.RefreshToken,
                ct
            );
        }

        await UpsertRootStateAsync(
            RootStateKeys.SoundRequestSpotifyAccessTokenExpiresAtUtc,
            expiresAtUtc.ToString("O"),
            ct
        );
    }

    private async Task<SpotifyProfileDto?> GetProfileAsync(string accessToken, CancellationToken ct)
    {
        SpotifyProfileDto? result = null;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try
            {
                using var httpClient = CreateHttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync(SpotifyMeUrl, ct);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    result = System.Text.Json.JsonSerializer.Deserialize<SpotifyProfileDto>(content);
                }
                else
                {
                    logger.LogWarning("Spotify profile API returned {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка получения профиля Spotify");
            }
        }

        return result;
    }

    private async Task PersistProfileAsync(SpotifyProfileDto? profile, CancellationToken ct)
    {
        var displayName = profile?.DisplayName ?? string.Empty;
        var userId = profile?.Id ?? string.Empty;
        var avatarUrl = profile?.Images?.FirstOrDefault()?.Url ?? string.Empty;
        var product = profile?.Product ?? string.Empty;

        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyDisplayName, displayName, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyUserId, userId, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyAvatarUrl, avatarUrl, ct);
        await UpsertRootStateAsync(RootStateKeys.SoundRequestSpotifyProduct, product, ct);
    }

    private async Task<string> GetRootStateValueAsync(string name, CancellationToken ct)
    {
        var result = string.Empty;

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var item = await db.RootState.AsNoTracking().SingleOrDefaultAsync(s => s.Name == name, ct);

        if (item != null)
        {
            result = item.Value ?? string.Empty;
        }

        return result;
    }

    private async Task UpsertRootStateAsync(string name, string value, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var existing = await db.RootState.SingleOrDefaultAsync(s => s.Name == name, ct);

        if (existing != null)
        {
            existing.Value = value;
            db.RootState.Update(existing);
        }
        else
        {
            await db.RootState.AddAsync(
                new RootState
                {
                    Name = name,
                    Value = value,
                },
                ct
            );
        }

        await db.SaveChangesAsync(ct);
    }

    private class SpotifyTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class SpotifyProfileDto
    {
        public string? Id { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        public List<SpotifyProfileImageDto>? Images { get; set; }

        public string? Product { get; set; }
    }

    private class SpotifyProfileImageDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
