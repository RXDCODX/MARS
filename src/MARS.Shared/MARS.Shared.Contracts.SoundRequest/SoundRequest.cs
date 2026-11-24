namespace MARS.Shared.Contracts.SoundRequest;

public enum SoundRequestPlaybackState
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
    SwitchingTrack = 3,
    WaitingForTrack = 4,
}
