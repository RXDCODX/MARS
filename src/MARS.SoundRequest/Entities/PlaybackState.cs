namespace MARS.SoundRequest.Entities;

public enum PlaybackState
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
    SwitchingTrack = 3,
    WaitingForTrack = 4,
}
