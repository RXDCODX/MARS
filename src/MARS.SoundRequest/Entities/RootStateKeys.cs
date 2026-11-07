using System.ComponentModel.DataAnnotations;

namespace MARS.SoundRequest.Entities;

public static class RootStateKeys
{
    public const string SoundRequestProvider = "SoundRequestProvider";
    public const string SoundRequestSpotifyClientId = "SoundRequestSpotifyClientId";
    public const string SoundRequestSpotifyClientSecret = "SoundRequestSpotifyClientSecret";
    public const string SoundRequestSpotifyRefreshToken = "SoundRequestSpotifyRefreshToken";
    public const string SoundRequestSpotifyAccessToken = "SoundRequestSpotifyAccessToken";
    public const string SoundRequestSpotifyAccessTokenExpiresAtUtc =
        "SoundRequestSpotifyAccessTokenExpiresAtUtc";
    public const string SoundRequestSpotifyDisplayName = "SoundRequestSpotifyDisplayName";
    public const string SoundRequestSpotifyUserId = "SoundRequestSpotifyUserId";
    public const string SoundRequestSpotifyAvatarUrl = "SoundRequestSpotifyAvatarUrl";
    public const string SoundRequestSpotifyProduct = "SoundRequestSpotifyProduct";
    public const string SoundRequestSpotifyDeviceId = "SoundRequestSpotifyDeviceId";
    public const string SoundRequestSpotifyOAuthState = "SoundRequestSpotifyOAuthState";
    public const string SoundRequestSpotifyRedirectUri = "SoundRequestSpotifyRedirectUri";
}
