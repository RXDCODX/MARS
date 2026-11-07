using System.Text.Json.Serialization;

namespace MARS.WaifuGacha.Entities;

public abstract class PrizeTypeAbstract
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("image")]
    public required string Image { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
