using MARS.Commands.Controllers;
using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Commands.Tests.Controllers;

/// <summary>
/// Действия контроллера команд.
///
/// Бизнес-ошибка возвращается как <c>Ok</c> с <c>Success = false</c>: HTTP-запрос
/// обработан, и «команды нет» — это ответ, а не сбой протокола. Отдельно
/// проверяется, что неизвестная команда не превращается в 500.
/// </summary>
public sealed class CommandsControllerTests
{
    [Fact]
    public void UserCommandsAreReturnedWithDescription()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance => instance.GetUserCommands(true, It.IsAny<CancellationToken>()))
            .Returns(["help - список команд"]);
        var controller = CreateController(service.Object);

        var result = Unwrap(controller.GetUserCommands(TestContext.Current.CancellationToken));

        Assert.True(result.Success);
        Assert.Equal(["help - список команд"], result.Result!);
    }

    [Fact]
    public void AdminCommandsAreReturnedWithDescription()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance => instance.GetAdminCommands(true, It.IsAny<CancellationToken>()))
            .Returns(["shutdown - остановка"]);
        var controller = CreateController(service.Object);

        var result = Unwrap(controller.GetAdminCommands(TestContext.Current.CancellationToken));

        Assert.True(result.Success);
        Assert.Equal(["shutdown - остановка"], result.Result!);
    }

    [Fact]
    public void UserListIsFilteredByRequestedPlatform()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetUserCommands(Platform.Twitch, true, It.IsAny<CancellationToken>())
            )
            .Returns(["title - смена названия"]);
        var controller = CreateController(service.Object);

        var result = Unwrap(
            controller.GetUserCommandsByPlatform(
                Platform.Twitch,
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal(["title - смена названия"], result.Result!);
        service.Verify(
            instance =>
                instance.GetUserCommands(Platform.Twitch, true, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void AdminListIsFilteredByRequestedPlatform()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetAdminCommands(Platform.Discord, true, It.IsAny<CancellationToken>())
            )
            .Returns(["setenv - переменная окружения"]);
        var controller = CreateController(service.Object);

        var result = Unwrap(
            controller.GetAdminCommandsByPlatform(
                Platform.Discord,
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal(["setenv - переменная окружения"], result.Result!);
        service.Verify(
            instance =>
                instance.GetAdminCommands(Platform.Discord, true, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void KnownCommandReturnsItsParameters()
    {
        var expected = new[]
        {
            new CommandParameterInfo { Name = "url", Type = CommandParameterType.String },
        };
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetCommandParameters("download", It.IsAny<CancellationToken>())
            )
            .Returns(expected);
        var controller = CreateController(service.Object);

        var result = Unwrap(
            controller.GetCommandParameters("download", TestContext.Current.CancellationToken)
        );

        Assert.True(result.Success);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void UnknownCommandIsReportedAsBusinessError()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetCommandParameters(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .Returns((CommandParameterInfo[]?)null);
        var controller = CreateController(service.Object);

        var action = controller.GetCommandParameters(
            "нет-такой",
            TestContext.Current.CancellationToken
        );

        var wrapper = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<OperationResult<CommandParameterInfo[]>>(wrapper.Value);
        Assert.False(result.Success);
        Assert.Contains("нет-такой", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutedCommandIsReturnedAsIs()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    "help",
                    It.IsAny<string>(),
                    Platform.Api,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CommandResult.Ok("список команд"));
        var controller = CreateController(service.Object);

        var action = await controller.ExecuteCommand(
            "help",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        var result = Unwrap(action);
        Assert.True(result.Success);
        Assert.Equal("список команд", result.Result!.Text);
    }

    [Fact]
    public async Task FailedCommandIsReportedWithoutHttpError()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                CommandResult.Fail("неизвестная команда", CommandErrorCode.UnknownCommand)
            );
        var controller = CreateController(service.Object);

        var action = await controller.ExecuteCommand(
            "нет-такой",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        var wrapper = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<OperationResult<CommandResult>>(wrapper.Value);
        Assert.False(result.Success);
        Assert.Equal("неизвестная команда", result.ErrorMessage);
    }

    /// <summary>
    /// Сбой исполнителя не должен выходить наружу исключением: вызывающий
    /// получает результат с текстом ошибки, а сервис не падает на одном плохом
    /// запросе.
    /// </summary>
    [Fact]
    public async Task ExecutorCrashBecomesFailedResult()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.ExecuteCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("исполнитель упал"));
        var controller = CreateController(service.Object);

        var action = await controller.ExecuteCommand(
            "help",
            string.Empty,
            TestContext.Current.CancellationToken
        );

        var result = Unwrap(action);
        Assert.False(result.Success);
        Assert.Contains("исполнитель упал", result.ErrorMessage);
    }

    /// <summary>
    /// Маршруты с описанием команд обязаны существовать: их зовёт клиент.
    /// </summary>
    /// <remarks>
    /// Путь проверяется не только вызовом метода, но и атрибутом маршрута.
    /// Вызов метода проходит, даже если атрибут уехал, а именно атрибут решает,
    /// отдаст ли сервер 404 живому браузеру. Такая рассинхронизация и случилась:
    /// метод <c>GetUserCommandsInfoByPlatform</c> в сервисе был, маршрута в
    /// контроллере — нет, и страница команд на стенде была пустой.
    /// </remarks>
    [Fact]
    public void InfoRoutesMatchTheShapeTheClientCalls()
    {
        var routes = typeof(CommandsController)
            .GetMethods()
            .SelectMany(method =>
                method
                    .GetCustomAttributes(
                        typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute),
                        true
                    )
                    .Cast<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
                    .Select(attribute => $"{method.Name}|{attribute.Template}")
            )
            .ToArray();

        Assert.Contains(
            routes,
            route => route == "GetUserCommandsInfoByPlatform|user/platform/{platform}/info"
        );
        Assert.Contains(
            routes,
            route => route == "GetAdminCommandsInfoByPlatform|admin/platform/{platform}/info"
        );
    }

    [Fact]
    public void UserInfoReturnsCommandsWithDescription()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetUserCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([new StubCommand("title", "смена названия", isAdmin: false)]);
        var controller = CreateController(service.Object);

        var result = Unwrap(
            controller.GetUserCommandsInfoByPlatform(
                Platform.Twitch,
                TestContext.Current.CancellationToken
            )
        );

        Assert.True(result.Success);
        Assert.Equal("title", Assert.Single(result.Result!).CommandName);
    }

    [Fact]
    public void AdminInfoReturnsOnlyAdminCommands()
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetAdminCommandsInfo(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns([new StubCommand("shutdown", "остановка", isAdmin: true)]);
        var controller = CreateController(service.Object);

        var result = Unwrap(
            controller.GetAdminCommandsInfoByPlatform(
                Platform.Twitch,
                TestContext.Current.CancellationToken
            )
        );

        Assert.True(result.Success);
        Assert.True(Assert.Single(result.Result!).IsAdminCommand);
    }

    /// <summary>
    /// Команда для проверки маршрутов описания.
    /// </summary>
    /// <remarks>
    /// <c>BaseCommand</c> абстрактный, а проверять нужно форму ответа: имя,
    /// описание и признак администраторской команды. Настоящая команда из набора
    /// для этого не нужна и связала бы тест с составом команд, который меняется
    /// независимо от маршрутов.
    /// </remarks>
    private sealed class StubCommand(string name, string description, bool isAdmin) : BaseCommand
    {
        public override string CommandName => name;

        public override string Description => description;

        public override bool IsAdminCommand => isAdmin;

        // Выполнение не проверяется: маршруты описания его не зовут, а настоящая
        // команда потребовала бы сервисов, которых у заглушки нет.
        public override Task<CommandResult> ExecuteAsync(
            Dictionary<string, object> parameters,
            Platform platform = Platform.None,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                CommandResult.Fail("Заглушка не выполняется.", CommandErrorCode.NotImplemented)
            );
    }

    private static CommandsController CreateController(ICommandService service) =>
        new(
            service,
            new ApiCommandService(service, NullLogger<ApiCommandService>.Instance),
            NullLogger<CommandsController>.Instance
        );

    private static OperationResult<T> Unwrap<T>(ActionResult<OperationResult<T>> action)
    {
        var wrapper = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<OperationResult<T>>(wrapper.Value);
    }
}
