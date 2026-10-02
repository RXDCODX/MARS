using System.Reflection;
using DSharpPlus;
using DSharpPlus.EventArgs;
using MARS.Discord.Services.Gateway;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DiscordConfig = MARS.Discord.Configuration.DiscordConfiguration;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Подключение к Discord и рассылка событий зарегистрированным обработчикам.
///
/// Подключение к самому Discord не проверяется: оно уходит в сеть, и тест
/// проходил бы или падал в зависимости от доступности внешнего API. Проверяется
/// то, что от него не зависит: отказ без токена, безопасная остановка без
/// подключения и главное — один упавший обработчик не роняет остальные, иначе
/// сломанная фича глушила бы все события бота.
/// </summary>
public class DiscordGatewayServiceTests
{
    private readonly List<string> _calls = [];
    private readonly DiscordGatewayService _service = Create(string.Empty);

    [Fact]
    public async Task MissingTokenDisablesIntegration()
    {
        await _service.StartAsync(TestContext.Current.CancellationToken);

        Assert.False(_service.IsConnected);
        Assert.Null(_service.Client);
    }

    [Fact]
    public async Task StopWithoutConnectionIsSafe()
    {
        await _service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(_service.IsConnected);
    }

    /// <summary>
    /// Без канала или текста сообщение не отправляется: в Discord есть канал 0
    /// (системный), и отправка туда ушла бы в пустоту.
    /// </summary>
    [Theory]
    [InlineData(0UL, "текст")]
    [InlineData(123UL, "")]
    public async Task InvalidParametersAreRejected(ulong channelId, string message)
    {
        var result = await _service.SendMessageAsync(
            channelId,
            message,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MessageHandlersAreInvoked()
    {
        _service.RegisterMessageCreatedHandler(
            (_, _) =>
            {
                _calls.Add("message");
                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleMessageCreatedAsync");

        Assert.Equal(["message"], _calls);
    }

    [Fact]
    public async Task VoiceStateHandlersAreInvoked()
    {
        _service.RegisterVoiceStateUpdatedHandler(
            (_, _) =>
            {
                _calls.Add("voice");
                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleVoiceStateUpdatedAsync");

        Assert.Equal(["voice"], _calls);
    }

    [Fact]
    public async Task InteractionHandlersAreInvoked()
    {
        _service.RegisterInteractionCreatedHandler(
            (_, _) =>
            {
                _calls.Add("interaction");
                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleInteractionCreatedAsync");

        Assert.Equal(["interaction"], _calls);
    }

    [Fact]
    public async Task ComponentInteractionHandlersAreInvoked()
    {
        _service.RegisterComponentInteractionCreatedHandler(
            (_, _) =>
            {
                _calls.Add("component");
                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleComponentInteractionCreatedAsync");

        Assert.Equal(["component"], _calls);
    }

    /// <summary>
    /// Упавший обработчик не должен глушить следующий: иначе одна сломанная
    /// команда отключала бы все остальные команды бота.
    /// </summary>
    [Fact]
    public async Task FailingHandlerDoesNotStopOthers()
    {
        _service.RegisterMessageCreatedHandler(
            (_, _) => throw new InvalidOperationException("сломано")
        );
        _service.RegisterMessageCreatedHandler(
            (_, _) =>
            {
                _calls.Add("второй");
                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleMessageCreatedAsync");

        Assert.Equal(["второй"], _calls);
    }

    [Fact]
    public async Task EventWithoutHandlersDoesNotThrow()
    {
        await InvokeAsync("HandleMessageCreatedAsync");
        await InvokeAsync("HandleVoiceStateUpdatedAsync");
        await InvokeAsync("HandleInteractionCreatedAsync");
        await InvokeAsync("HandleComponentInteractionCreatedAsync");
    }

    /// <summary>
    /// Обработчики рассылаются копией списка: добавление нового во время рассылки
    /// не должно ломать перебор уже зарегистрированных.
    /// </summary>
    [Fact]
    public async Task HandlerRegisteredDuringDispatchDoesNotBreakTheLoop()
    {
        _service.RegisterMessageCreatedHandler(
            (_, _) =>
            {
                _calls.Add("первый");
                _service.RegisterMessageCreatedHandler(
                    (_, _) =>
                    {
                        _calls.Add("добавленный");
                        return Task.CompletedTask;
                    }
                );

                return Task.CompletedTask;
            }
        );

        await InvokeAsync("HandleMessageCreatedAsync");

        Assert.Equal(["первый"], _calls);
    }

    /// <summary>
    /// Обработчики событий приватные и вызываются Discord: в тесте они
    /// вызываются напрямую. Клиент и аргументы передаются нулевыми — зарегистрированные
    /// обработчики их не читают, а настоящие типы Discord требуют живого клиента.
    /// </summary>
    private Task InvokeAsync(string method) =>
        (Task)
            typeof(DiscordGatewayService)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_service, [null, null])!;

    private static DiscordGatewayService Create(string token) =>
        new(
            Options.Create(new DiscordConfig { Token = token }),
            NullLogger<DiscordGatewayService>.Instance
        );
}
