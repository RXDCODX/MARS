using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services.ChannelRewards;

public class ChannelRewardsService : IChannelRewardsService, IDisposable
{
    public IRewardsCacheService RewardsCacheService { get; }

    public bool IsServiceActive { get; set; } = true;

    private readonly IOptionsMonitor<TwitchRewardsOptions> _rewardsOptionsMonitor;
    private readonly ITwitchAPI _api;
    private readonly TokenService _tokenService;
    private readonly ILogger<ChannelRewardsService> _logger;

    public ChannelRewardsService(
        ITwitchAPI api,
        TokenService tokenService,
        ILogger<ChannelRewardsService> logger,
        IOptionsMonitor<TwitchRewardsOptions> rewardsOptionsMonitor
    )
    {
        _api = api;
        _tokenService = tokenService;
        _logger = logger;
        _rewardsOptionsMonitor =
            rewardsOptionsMonitor ?? throw new ArgumentNullException(nameof(rewardsOptionsMonitor));
        RewardsCacheService = new RewardsCacheService(GetRewardsDirectAsync, logger);
    }

    public async Task<string?> CreateRewardAsync(CreateCustomRewardsRequest request)
    {
        if (!IsServiceActive)
        {
            _logger.LogWarning("ChannelRewardsService выключен");
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(_tokenService.Token?.AccessToken);

        try
        {
            var response = await _api.Helix.ChannelPoints.CreateCustomRewardsAsync(
                TwitchConstants.ChannelId,
                request,
                _tokenService.Token.AccessToken
            );

            var created = response.Data.FirstOrDefault();
            if (created == null)
            {
                _logger.LogError("Не удалось создать награду канала: пустой ответ");
                return null;
            }

            _logger.LogInformation(
                "Создана награда канала: {Title} (Id: {Id}, Cost: {Cost})",
                created.Title,
                created.Id,
                created.Cost
            );

            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return null;
        }
    }

    public Task<IEnumerable<CustomReward>?> GetRewardsAsync() =>
        RewardsCacheService.GetRewardsAsync();

    private async Task<IEnumerable<CustomReward>?> GetRewardsDirectAsync()
    {
        if (!IsServiceActive)
        {
            _logger.LogWarning("ChannelRewardsService выключен");
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(_tokenService.Token?.AccessToken);

        try
        {
            var response = await _api.Helix.ChannelPoints.GetCustomRewardAsync(
                TwitchConstants.ChannelId,
                null,
                false,
                _tokenService.Token.AccessToken
            );

            _logger.LogInformation("Получено {Count} наград канала", response.Data.Length);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return null;
        }
    }

    public async Task<bool> DeleteRewardAsync(string rewardId)
    {
        if (!IsServiceActive)
        {
            _logger.LogWarning("ChannelRewardsService выключен");
            return false;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rewardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(_tokenService.Token?.AccessToken);

        try
        {
            await _api.Helix.ChannelPoints.DeleteCustomRewardAsync(
                TwitchConstants.ChannelId,
                rewardId,
                _tokenService.Token.AccessToken
            );

            _logger.LogInformation("Удалена награда канала: {RewardId}", rewardId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return false;
        }
    }

    public async Task<CustomReward?> GetRewardByIdAsync(string rewardId)
    {
        if (!IsServiceActive)
        {
            _logger.LogWarning("ChannelRewardsService выключен");
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rewardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(_tokenService.Token?.AccessToken);

        try
        {
            var response = await _api.Helix.ChannelPoints.GetCustomRewardAsync(
                TwitchConstants.ChannelId,
                [rewardId],
                true,
                _tokenService.Token.AccessToken
            );

            return response.Data.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return null;
        }
    }

    public async Task<bool> UpdateRewardAsync(string rewardId, UpdateCustomRewardRequest request)
    {
        if (!IsServiceActive)
        {
            _logger.LogWarning("ChannelRewardsService выключен");
            return false;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rewardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(_tokenService.Token?.AccessToken);

        try
        {
            await _api.Helix.ChannelPoints.UpdateCustomRewardAsync(
                TwitchConstants.ChannelId,
                rewardId,
                request,
                _tokenService.Token?.AccessToken
            );

            _logger.LogInformation("Обновлена награда канала: {RewardId}", rewardId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return false;
        }
    }

    public bool? GetEnabledOverrideForCost(int cost)
    {
        var dict = _rewardsOptionsMonitor.CurrentValue?.EnabledByCost;
        if (dict == null)
        {
            return null;
        }

        return dict.TryGetValue(cost, out var val) ? val : null;
    }

    public void Dispose()
    {
        // Nothing to dispose currently
    }
}
