using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Domain.Entities;

public static class RootStateKeys
{
    public const string WTelegramMtProxyUrl = "WTelegramMtProxyUrl";
    public const string WTelegramProxyUrl = "WTelegramProxyUrl";

    // Google Photos Keys
    public const string GooglePhotosAccessToken = "GooglePhotosAccessToken";
    public const string GooglePhotosRefreshToken = "GooglePhotosRefreshToken";
    public const string GooglePhotosAccessTokenExpiresAtUtc = "GooglePhotosAccessTokenExpiresAtUtc";
    public const string GooglePhotosOAuthState = "GooglePhotosOAuthState";
    public const string GooglePhotosIsAuthorized = "GooglePhotosIsAuthorized";
}
