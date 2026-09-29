using System.Text.Json.Serialization;

namespace MARS.Telegram.Entities;

public class TelegramDiscordBindingCreateRequest
{
    public long TelegramChannelId { get; set; }

    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public ulong DiscordChannelId { get; set; }
}
