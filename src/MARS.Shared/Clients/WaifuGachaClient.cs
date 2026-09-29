using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент MARS.WaifuGacha по его internal API.
/// Оба эндпоинта закрыты политикой <c>ServiceApiKey</c>, ключ подставляет
/// <see cref="ServiceHttpClientBase"/>.
/// </summary>
public class WaifuGachaClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<WaifuGachaClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IWaifuGachaClient
{
    public override string ServiceEndpoint => endpoints.Value.WaifuGacha;

    public async Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        string? result = null;

        if (!string.IsNullOrWhiteSpace(twitchUserId))
        {
            var name = await GetAsync<string>(
                $"api/internal/husbands/{Uri.EscapeDataString(twitchUserId)}/waifu-name",
                cancellationToken
            );

            if (!string.IsNullOrWhiteSpace(name))
            {
                result = name;
            }
        }

        return result;
    }

    public Task<string?> GetAutoHelloMessageAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        Task<string?> result;

        if (string.IsNullOrWhiteSpace(twitchUserId) || string.IsNullOrWhiteSpace(displayName))
        {
            result = Task.FromResult<string?>(null);
        }
        else
        {
            result = GetAutoHelloCoreAsync(twitchUserId, displayName, cancellationToken);
        }

        return result;
    }

    private async Task<string?> GetAutoHelloCoreAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken
    )
    {
        var message = await PostAsync<AutoHelloRequest, string>(
            $"api/internal/auto-hello/{Uri.EscapeDataString(twitchUserId)}",
            new AutoHelloRequest(displayName),
            cancellationToken
        );

        return string.IsNullOrWhiteSpace(message) ? null : message;
    }

    public Task<bool> ToggleAutoHelloAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        Task<bool> result;

        if (string.IsNullOrWhiteSpace(twitchUserId))
        {
            result = Task.FromResult(false);
        }
        else
        {
            result = ToggleAutoHelloCoreAsync(twitchUserId, cancellationToken);
        }

        return result;
    }

    private async Task<bool> ToggleAutoHelloCoreAsync(
        string twitchUserId,
        CancellationToken cancellationToken
    )
    {
        var enabled = await PostAsync<AutoHelloToggleRequest, bool>(
            $"api/internal/auto-hello/{Uri.EscapeDataString(twitchUserId)}/toggle",
            new AutoHelloToggleRequest(true),
            cancellationToken
        );

        return enabled;
    }

    public record AutoHelloRequest(string DisplayName);

    public record AutoHelloToggleRequest(bool Enabled);
}
