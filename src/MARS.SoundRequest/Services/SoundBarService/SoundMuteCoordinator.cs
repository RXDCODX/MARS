namespace MARS.SoundRequest.Services.SoundBarService;

public class SoundMuteCoordinator
{
    private readonly SoundBarFactory? _soundBarFactory;
    private readonly StateManager _stateManager;
    private readonly ILogger<SoundMuteCoordinator> _logger;
    private readonly Func<ISoundBar>? _soundBarProvider;

    public SoundMuteCoordinator(
        Func<ISoundBar> soundBarProvider,
        StateManager stateManager,
        ILogger<SoundMuteCoordinator> logger
    )
    {
        _soundBarProvider = soundBarProvider;
        _stateManager = stateManager;
        _logger = logger;
    }

    public SoundMuteCoordinator(
        SoundBarFactory soundBarFactory,
        StateManager stateManager,
        ILogger<SoundMuteCoordinator> logger
    )
    {
        _soundBarFactory = soundBarFactory;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task MuteAsync(params string[] args)
    {
        try
        {
            var sb = _soundBarProvider?.Invoke() ?? _soundBarFactory!.CreateSoundBar();
            await sb.Mute(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call SoundBar.Mute");
        }

        try
        {
            var state = await _stateManager.GetStateAsync();
            if (state.State == Entities.PlaybackState.Playing)
            {
                await _stateManager.SetPausedAsync(true);
                await _stateManager.SetPausedByMuteAsync(true);
            }

            await _stateManager.SetMutedAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to coordinate mute actions");
        }
    }

    public async Task UnmuteAsync()
    {
        try
        {
            var sb = _soundBarProvider?.Invoke() ?? _soundBarFactory!.CreateSoundBar();
            await sb.Unmute();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call SoundBar.Unmute");
        }

        try
        {
            await _stateManager.SetMutedAsync(false);

            var state = await _stateManager.GetStateAsync();
            if (state.PausedByMute)
            {
                await _stateManager.SetPausedAsync(false);
                await _stateManager.SetPausedByMuteAsync(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to coordinate unmute actions");
        }
    }
}
