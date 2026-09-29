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

public class PrizeType : PrizeTypeAbstract;

public class FumoPrizeType : PrizeTypeAbstract;

public class FrogPrizeType : PrizeTypeAbstract;

public class MikuPrizeType : PrizeTypeAbstract;
