namespace MARS.SoundRequest.Entities;

public class SpotifyAuthCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; } = DateTime.UnixEpoch;
    public string DeviceId { get; set; } = string.Empty;
}

public class SpotifyAuthStartResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class SpotifyAuthStartRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

public class SpotifyAuthCompleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Product { get; set; }
}

public class SpotifyAuthStatusResult
{
    public bool IsLinked { get; set; }
    public bool HasClientCredentials { get; set; }
    public string? DisplayName { get; set; }
    public string? UserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Product { get; set; }
    public string? DeviceId { get; set; }
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SpotifyAccessTokenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UnixEpoch;
    public string DeviceId { get; set; } = string.Empty;
}
