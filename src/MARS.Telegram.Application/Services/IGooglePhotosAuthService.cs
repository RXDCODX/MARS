using MARS.Telegram.Domain.Entities;

namespace MARS.Telegram.Application.Services;

public interface IGooglePhotosAuthService
{
    Task<string> GetAuthorizationUrlAsync(CancellationToken ct);
    Task<GooglePhotosTokens?> ExchangeCodeForTokenAsync(string code, CancellationToken ct);
    Task<bool> IsAuthorizedAsync(CancellationToken ct);
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct);
}
