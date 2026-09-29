using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using Timer = System.Timers.Timer;

namespace MARS.TwitchCore.Services.AutoInfoFetch;

public class AutoRewardInfoFetcher(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<AutoRewardInfoFetcher> logger
) : BackgroundService
{
    private Timer? _timer;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        if (!stoppingToken.IsCancellationRequested)
        {
            await FetchRewardInfoAsync();

            _timer = new Timer(TimeSpan.FromMinutes(10));
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (!_stoppingToken.IsCancellationRequested)
            {
                await FetchRewardInfoAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }

    private async Task FetchRewardInfoAsync()
    {
        try
        {
            var twitchAlerts = await api.Helix.ChannelPoints.GetCustomRewardAsync(
                TwitchConstants.ChannelId,
                null,
                false,
                tokenService.Token?.AccessToken
            );

            logger.LogInformation(
                "Получено {Count} наград из Twitch API",
                twitchAlerts.Data.Length
            );
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }

    public override void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
