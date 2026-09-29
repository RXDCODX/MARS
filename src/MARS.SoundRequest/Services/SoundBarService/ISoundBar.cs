namespace MARS.SoundRequest.Services.SoundBarService;

public interface ISoundBar
{
    public Task Mute(params string[] args);
    public Task Unmute();
    public Task<string> GetBagCount();
    public Task<bool> CheckHealthAsync();
}
