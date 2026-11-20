using System.Text.Json.Serialization;

namespace MARS.Shared.Contracts.Models;

public class TunaMusicDTO
{
    [JsonPropertyName("data")]
    public required TunaMusicData Data { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Date { get; set; }
}
