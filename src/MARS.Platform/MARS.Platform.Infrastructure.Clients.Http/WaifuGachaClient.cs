using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

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

    public Task<CollectionInventory?> GetFumoInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<CollectionInventory>(
            $"api/internal/collections/fumo?twitchId={Uri.EscapeDataString(twitchUserId)}",
            cancellationToken
        );
    }

    public Task<CollectionInventory?> GetMikuInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<CollectionInventory>(
            $"api/internal/collections/miku?twitchId={Uri.EscapeDataString(twitchUserId)}",
            cancellationToken
        );
    }

    public Task<ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriTitleRef>(
            "api/internal/shikimori/random-anime",
            cancellationToken
        );
    }

    public Task<ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriTitleRef>(
            "api/internal/shikimori/random-manga",
            cancellationToken
        );
    }

    public Task<List<MikuTrackDto>?> GetMikuMondayTracksAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<List<MikuTrackDto>>("api/internal/miku-monday/tracks", cancellationToken);
    }

    public Task<MikuMondayResult?> PickMikuMondayTrackAsync(
        string twitchUserId,
        string displayName,
        bool isStreamer,
        CancellationToken cancellationToken = default
    )
    {
        return PostAsync<MikuMondayPickRequest, MikuMondayResult>(
            "api/internal/miku-monday/tracks/random",
            new MikuMondayPickRequest
            {
                TwitchUserId = twitchUserId,
                DisplayName = displayName,
                IsStreamer = isStreamer,
            },
            cancellationToken
        );
    }

    public record AutoHelloRequest(string DisplayName);

    public async Task<string?> RollWaifuAsync(
        string hostName,
        CancellationToken cancellationToken = default
    )
    {
        var result = await PostAsync<WaifuRollRequest, OperationResult<string>>(
            "api/internal/rolls/waifu",
            new WaifuRollRequest { HostName = hostName },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public async Task<string?> RollMikuAsync(CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<object, OperationResult<string>>(
            "api/internal/rolls/miku",
            new { },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public async Task<string?> RollFumoAsync(CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<object, OperationResult<string>>(
            "api/internal/rolls/fumo",
            new { },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public async Task<string?> RollFrogAsync(CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<object, OperationResult<string>>(
            "api/internal/rolls/frog",
            new { },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public async Task<string?> MergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await PostAsync<object, OperationResult<string>>(
            $"api/internal/husbands/{Uri.EscapeDataString(twitchUserId)}/merge",
            new { },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public async Task<string?> UnmergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await PostAsync<object, OperationResult<string>>(
            $"api/internal/husbands/{Uri.EscapeDataString(twitchUserId)}/unmerge",
            new { },
            cancellationToken
        );
        return result is null ? null
            : result.Success ? result.Result
            : result.ErrorMessage;
    }

    public record WaifuRollRequest
    {
        public string HostName { get; set; } = string.Empty;
    }

    public record AutoHelloToggleRequest(bool Enabled);

    public record MikuMondayPickRequest
    {
        public string TwitchUserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsStreamer { get; set; }
    }
}
