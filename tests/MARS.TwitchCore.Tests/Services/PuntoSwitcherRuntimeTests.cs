using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.PuntoSwitcher;
using Moq;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// PuntoSwitcher на живом клиенте Twitch.
///
/// Сервис молча переписывает сообщения чата, поэтому проверяются две вещи, которые
/// не видны в тестах самого преобразования: состояние фильтра действительно
/// читается из базы, а отказ валидации (чёрный список, другой канал) не приводит к
/// правке сообщения.
/// </summary>
public class PuntoSwitcherRuntimeTests
{
    private readonly TwitchTestDbContextFactory _factory = new();

    /// <summary>
    /// Выключатель хранится в базе, и после старта сервис обязан его уважать:
    /// иначе отключённый фильтр снова включался бы при каждом перезапуске.
    /// </summary>
    [Fact]
    public async Task FilterFlagIsReadFromRootState()
    {
        await SeedFilterAsync("false");
        var service = new PuntoSwitcherService(
            Mock.Of<ITwitchClient>(),
            _factory,
            PassingValidationService.Instance
        );

        await ExecuteAsync(service);

        Assert.False(service.IsFilterEnabled);
    }

    /// <summary>
    /// Выключенный фильтр не трогает сообщения вовсе.
    /// </summary>
    [Fact]
    public async Task DisabledFilterKeepsMessage()
    {
        await SeedFilterAsync("false");
        var service = new PuntoSwitcherService(
            Mock.Of<ITwitchClient>(),
            _factory,
            PassingValidationService.Instance
        );
        await ExecuteAsync(service);

        var args = PassingValidationService.Message("ghbdtn");
        await OnMessageReceived(service, args);

        Assert.Equal("ghbdtn", args.ChatMessage.Message);
    }

    /// <summary>
    /// Включённый фильтр исправляет опечатку раскладки прямо в сообщении чата —
    /// именно это он и делает на стриме.
    /// </summary>
    [Fact]
    public async Task EnabledFilterRewritesMessage()
    {
        await SeedFilterAsync("true");
        var service = new PuntoSwitcherService(
            Mock.Of<ITwitchClient>(),
            _factory,
            PassingValidationService.Instance
        );
        await ExecuteAsync(service);

        var args = PassingValidationService.Message("ghbdtn");
        await OnMessageReceived(service, args);

        Assert.Equal("привет", args.ChatMessage.Message);
    }

    /// <summary>
    /// Отклонённое валидацией сообщение остаётся как есть: иначе фильтр правил бы
    /// сообщения, которые не должен трогать.
    /// </summary>
    [Fact]
    public async Task RejectedMessageIsNotRewritten()
    {
        await SeedFilterAsync("true");
        var service = new PuntoSwitcherService(
            Mock.Of<ITwitchClient>(),
            _factory,
            PassingValidationService.Rejecting("сообщение в чёрном списке")
        );
        await ExecuteAsync(service);

        var args = PassingValidationService.Message("ghbdtn");
        await OnMessageReceived(service, args);

        Assert.Equal("ghbdtn", args.ChatMessage.Message);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task SeedFilterAsync(string value)
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        db.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.PuntoSwitcherFilterEnabled,
                Value = value,
                TypeDescription = "bool",
            }
        );
        await db.SaveChangesAsync(Token);
    }

    private static Task ExecuteAsync(PuntoSwitcherService service)
    {
        var method = typeof(PuntoSwitcherService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(service, [TestContext.Current.CancellationToken])!;
    }

    private static Task OnMessageReceived(PuntoSwitcherService service, OnMessageReceivedArgs args)
    {
        var method = typeof(PuntoSwitcherService).GetMethod(
            "OnMessageReceived",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(service, [null, args])!;
    }
}
