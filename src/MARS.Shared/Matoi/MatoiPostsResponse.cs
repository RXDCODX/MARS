using System.Text.Json.Serialization;

namespace MARS.Shared.Matoi;

/// <summary>
/// Конверт ответа matoi: <c>{"success":true,"provider":…,"count":…,"posts":[…]}</c>.
/// </summary>
public sealed class MatoiPostsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Провайдер, как его понял matoi.</summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Сколько постов вернулось. Не равно запрошенному <c>limit</c>.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("posts")]
    public MatoiPost[]? Posts { get; set; }
}
