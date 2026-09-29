namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Cross-microservice lookup for waifu names.
/// Implemented by a RabbitMQ-backed service that queries MARS.WaifuGacha.
/// </summary>
public interface IWaifuLookupService
{
    Task<string?> GetWaifuNameForUserAsync(string twitchUserId, CancellationToken ct = default);
}
