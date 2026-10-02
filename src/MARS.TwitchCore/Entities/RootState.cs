using System.ComponentModel.DataAnnotations;

namespace MARS.TwitchCore.Entities;

public class RootState
{
    [Key]
    public required string Name { get; set; }

    public string Value { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TypeDescription { get; set; } = string.Empty;
}

public static class RootStateKeys
{
    public const string PuntoSwitcherFilterEnabled = "PuntoSwitcherFilterEnabled";
    public const string TtsFilterEnabled = "TtsFilterEnabled";

    /// <summary>
    /// ID Discord-канала для пересылки сообщений из чатов теккен-стримов
    /// (<see cref="Services.TekkenStreams.TekkenStreamsDiscordForwarderService"/>).
    /// Пусто или 0 — пересылка выключена.
    /// </summary>
    public const string TekkenStreamsDiscordChannelId = "TekkenStreamsDiscordChannelId";
}
