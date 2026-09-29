using MARS.Telegram.Entities;

namespace MARS.Telegram.Services;

public interface IGooglePhotosAuthService
{
    Task<string> GetAuthorizationUrlAsync(CancellationToken ct);
    Task<GooglePhotosTokens?> ExchangeCodeForTokenAsync(string code, CancellationToken ct);
    Task<bool> IsAuthorizedAsync(CancellationToken ct);
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct);
}
