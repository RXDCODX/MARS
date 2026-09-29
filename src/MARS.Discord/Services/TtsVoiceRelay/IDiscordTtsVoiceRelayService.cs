namespace MARS.Discord.Services.TtsVoiceRelay;

public interface IDiscordTtsVoiceRelayService
{
    bool IsVoiceRoutingEnabled { get; }

    Task PlaySpeechAsync(
        string voiceName,
        string text,
        string? additionalText = null,
        CancellationToken cancellationToken = default
    );
}
