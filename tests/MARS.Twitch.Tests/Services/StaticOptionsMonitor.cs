using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.ChannelRewards;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Tests.Services;

/// <summary>
/// Монитор опций с фиксированным значением: проверяется чтение переопределения по
/// стоимости, а не подписка на конфигурацию.
/// </summary>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
