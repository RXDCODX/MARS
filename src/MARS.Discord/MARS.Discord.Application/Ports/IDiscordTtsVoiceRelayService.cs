namespace MARS.Discord.Application.Ports;

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
