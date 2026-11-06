using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Entitys = MARS.Commands.Services.Entitys;

namespace MARS.Commands.Tests.Adapters;

/// <summary>
/// Выполнение команды через API-адаптер: длинный ответ обрезается, ошибка
/// исполнителя не превращается в 500, а неизвестная команда не доходит до
/// исполнителя вовсе.
/// </summary>
public sealed class ApiCommandServiceExecutionTests
{
    [Fact]
    public async Task EmptyCommandNameIsRejectedWithoutCallingExecutor()
    {
        var service = new Mock<ICommandService>(MockBehavior.Strict);
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        var result = await adapter.ExecuteCommandAsync(
            "  ",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.UnknownCommand, result.ErrorCode);
    }

    [Fact]
    public async Task LongAnswerIsTrimmedBeforeReturning()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Entitys.Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CommandResult.Ok(new string('z', 12_000)));
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        var result = await adapter.ExecuteCommandAsync(
            "catisa",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Success);
        Assert.EndsWith("[Ответ обрезан...]", result.Text);
    }

    [Fact]
    public async Task BadArgumentsAreReportedAsBusinessError()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Entitys.Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ArgumentException("не хватает параметра"));
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        var result = await adapter.ExecuteCommandAsync(
            "rollfumo",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.BadArguments, result.ErrorCode);
        Assert.Contains("не хватает параметра", result.Text);
    }

    [Fact]
    public async Task ExecutorFailureIsWrappedIntoResult()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Entitys.Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("исполнитель упал"));
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        var result = await adapter.ExecuteCommandAsync(
            "help",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.Failed, result.ErrorCode);
        Assert.Contains("исполнитель упал", result.Text);
    }

    [Fact]
    public void AdminAndAvailabilityAreDelegatedToExecutor()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance => instance.IsAdminCommand("shutdown", It.IsAny<CancellationToken>()))
            .Returns(true);
        service
            .Setup(instance => instance.IsCommandAvailable("shutdown", Entitys.Platform.Api))
            .Returns(false);
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        Assert.True(adapter.IsAdminCommand("shutdown"));
        Assert.False(adapter.IsCommandAvailable("shutdown"));
    }

    /// <summary>
    /// Описание команды API-аудитории не показывает: у вызывающего нет прав
    /// проверять, и в списке остаются только имена.
    /// </summary>
    [Fact]
    public void ListsArePassedThroughFromExecutor()
    {
        var service = new Mock<ICommandService>();
        service.Setup(instance => instance.GetUserCommands(Entitys.Platform.Api)).Returns(["help"]);
        service
            .Setup(instance => instance.GetAdminCommands(Entitys.Platform.Api))
            .Returns(["shutdown"]);
        service
            .Setup(instance =>
                instance.GetCommandParameters("rollfumo", It.IsAny<CancellationToken>())
            )
            .Returns([
                new CommandParameterInfo { Name = "персонаж", Type = CommandParameterType.String },
            ]);
        service
            .Setup(instance =>
                instance.GetUserCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([new TitleChangeStreamTitleCommand()]);
        service
            .Setup(instance =>
                instance.GetAdminCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([new TitleChangeStreamTitleCommand()]);
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        Assert.Equal(["help"], adapter.GetUserCommands(Entitys.Platform.Api));
        Assert.Equal(["shutdown"], adapter.GetAdminCommands(Entitys.Platform.Api));
        Assert.Single(adapter.GetCommandParameters("rollfumo")!);
        Assert.Single(adapter.GetUserCommandsInfo(Entitys.Platform.Api));
        Assert.Single(adapter.GetAdminCommandsInfo(Entitys.Platform.Api));
    }

    /// <summary>
    /// Запрошенная платформа обязана влиять на ответ: метод принимает
    /// <c>Entitys.Platform</c> параметром, и подставлять вместо него константу Api —
    /// значит отдать Twitch-аудитории Discord-команды.
    /// </summary>
    [Fact]
    public void RequestedPlatformIsHonoured()
    {
        var twitchCommand = new TitleChangeStreamTitleCommand();
        var discordCommand = new DiscordOnlyCommand();
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetUserCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([twitchCommand, discordCommand]);
        service
            .Setup(instance =>
                instance.GetAdminCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([twitchCommand, discordCommand]);
        var adapter = new ApiCommandService(service.Object, NullLogger<ApiCommandService>.Instance);

        Assert.Equal([twitchCommand], adapter.GetUserCommandsInfo(Entitys.Platform.Twitch));
        Assert.Equal([discordCommand], adapter.GetUserCommandsInfo(Entitys.Platform.Discord));
        Assert.Equal([twitchCommand], adapter.GetAdminCommandsInfo(Entitys.Platform.Twitch));
        Assert.Equal([discordCommand], adapter.GetAdminCommandsInfo(Entitys.Platform.Discord));
    }

    private sealed class DiscordOnlyCommand : BaseCommand
    {
        public override string CommandName => "discord-only";

        public override string Description => "Команда только для Discord";

        public override bool IsAdminCommand => false;

        public override Entitys.Platform[] AvailablePlatforms => [Entitys.Platform.Discord];

        public override Task<CommandResult> ExecuteAsync(
            Dictionary<string, object> parameters,
            Entitys.Platform platform = Entitys.Platform.None,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CommandResult.Ok("discord"));
    }
}
