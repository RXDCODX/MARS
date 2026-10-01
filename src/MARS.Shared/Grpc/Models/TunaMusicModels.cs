using System.Text.Json.Serialization;

namespace MARS.Shared.Grpc.Models;

public class TunaMusicData
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("cover")]
    public required string Cover { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("artists")]
    public required string[] Artists { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("progress")]
    public ulong Progression { get; set; }

    [JsonPropertyName("duration")]
    public ulong Duration { get; set; }

    [JsonPropertyName("album_url")]
    public required string AlbumUrl { get; set; }
}

public class TunaMusicDTO
{
    [JsonPropertyName("data")]
    public required TunaMusicData Data { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Date { get; set; }
}
