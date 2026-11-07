using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.TwitchFollowers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Время жизни приложения, которым можно управлять в тесте: без него сервисы,
/// регистрирующие обработчики на старте, не запускались бы.
/// </summary>
internal sealed class TestLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => _stopping.Token;

    public void StopApplication() => _stopping.Cancel();
}
