namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

/// <summary>
/// Настройки gRPC-эндпоинтов. Отдельная секция нужна потому, что
/// ServiceEndpoints содержит только HTTP URL и отражается Gateway/Swagger.
/// </summary>
public class GrpcOptions
{
    public const string SectionName = "Grpc";

    public int HttpPort { get; set; } = 8080;

    public int GrpcPort { get; set; } = 8081;

    /// <summary>
    /// Явные адреса gRPC (http://service:8081). Правило «REST-порт + 1» не
    /// используется: на хосте порты разнесены по схеме launch settings.
    /// </summary>
    public Dictionary<string, string> Endpoints { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AudioController"] = "http://host.docker.internal:30696",
            ["Alerts"] = "http://alerts:8081",
            ["Commands"] = "http://commands:8081",
            ["Discord"] = "http://discord:8081",
            ["MediaStorage"] = "http://media-storage:8081",
            ["Shikimori"] = "http://shikimori:8081",
            ["SoundRequest"] = "http://sound-request:8081",
            ["Telegram"] = "http://telegram:8081",
            ["Tts"] = "http://tts:8081",
            ["TwitchCore"] = "http://twitch-core:8081",
            ["WaifuGacha"] = "http://waifu-gacha:8081",
        };
}
