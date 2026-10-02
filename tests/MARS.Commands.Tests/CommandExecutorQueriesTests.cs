using MARS.Commands.Services;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Commands.Tests;

/// <summary>
/// Выдача реестра команд: списки пользовательских и административных команд,
/// параметры по имени и по алиасу, признак админской команды и доступность на
/// платформе.
///
/// Проверяется на настоящем исполнителе с настоящей фабрикой и настоящими
/// командами: на подставных <c>BaseCommand</c> проверялся бы мок, а не
/// правила отбора, которые как раз и сломались бы незаметно.
/// </summary>
public sealed class CommandExecutorQueriesTests
{
    [Fact]
    public async Task UserAndAdminListsAreDisjoint()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var users = executor.GetUserCommands(
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var admins = executor.GetAdminCommands(
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );

        Assert.NotEmpty(users);
        Assert.NotEmpty(admins);
        Assert.Empty(users.Intersect(admins));
    }

    [Fact]
    public async Task DescriptionsAreAddedOnDemand()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var shortList = executor.GetUserCommands(
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var fullList = executor.GetUserCommands(
            isAddDescription: true,
            TestContext.Current.CancellationToken
        );

        Assert.Contains(fullList, entry => entry.Contains(" - "));
        Assert.Equal(shortList.Length, fullList.Length);
    }

    [Fact]
    public async Task PlatformFiltersNarrowTheList()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var all = executor.GetUserCommands(
            Platform.All,
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var twitchOnly = executor.GetUserCommands(
            Platform.Twitch,
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var adminAll = executor.GetAdminCommands(
            Platform.All,
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );

        Assert.NotEmpty(twitchOnly);
        Assert.True(
            twitchOnly.Length <= all.Length,
            "Фильтр по платформе вернул больше команд, чем их есть вообще."
        );
        Assert.NotEmpty(adminAll);
    }

    [Fact]
    public async Task ParametersAreFoundByNameAndByAlias()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var withParameters = CommandWithParametersAndAlias(executor);
        Assert.NotNull(withParameters);

        var byName = executor.GetCommandParameters(
            withParameters.CommandName,
            TestContext.Current.CancellationToken
        );
        var byAlias = executor.GetCommandParameters(
            withParameters.Aliases[0],
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(byName);

        // Сравниваются не сами объекты: Parameters создаётся заново на каждый
        // вызов, и ссылочное равенство здесь всегда давало бы false.
        Assert.Equal(Signature(byName!), Signature(byAlias!));
        Assert.Null(
            executor.GetCommandParameters(
                "такой команды нет",
                TestContext.Current.CancellationToken
            )
        );
        Assert.Null(executor.GetCommandParameters("   ", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdminFlagIsAnsweredForEveryRegisteredCommand()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        Assert.True(executor.IsAdminCommand("shutdown", TestContext.Current.CancellationToken));
        Assert.False(executor.IsAdminCommand("help", TestContext.Current.CancellationToken));
        Assert.False(executor.IsAdminCommand("   ", TestContext.Current.CancellationToken));
        Assert.False(executor.IsAdminCommand("неизвестная", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AvailabilityFollowsDeclaredPlatforms()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        Assert.True(executor.IsCommandAvailable("title", Platform.Twitch));
        Assert.False(executor.IsCommandAvailable("неизвестная", Platform.Twitch));
        Assert.False(executor.IsCommandAvailable("   ", Platform.Twitch));
    }

    /// <summary>
    /// <c>GetUserCommandsInfo</c> и <c>GetAdminCommandsInfo</c> отдают объекты
    /// команд, а не строки: ими пользуется выдача inline-кнопок, и ей нужен
    /// сам объект с его параметрами и платформами.
    /// </summary>
    [Fact]
    public async Task CommandInfosAreFilteredByPlatformAndRole()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var users = executor.GetUserCommandsInfo(
            Platform.All,
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var admins = executor.GetAdminCommandsInfo(
            Platform.All,
            isAddDescription: false,
            TestContext.Current.CancellationToken
        );
        var inline = executor.GetInlineCommandsInfo(
            Platform.All,
            TestContext.Current.CancellationToken
        );

        Assert.NotEmpty(users);
        Assert.NotEmpty(admins);
        Assert.Empty(users.Intersect(admins));
        Assert.All(
            inline,
            command => Assert.True(command.SupportsInline || command.SupportsMediaInline)
        );
        Assert.All(users, command => Assert.False(command.IsAdminCommand));
    }

    /// <summary>
    /// Список под одну платформу обязан быть подмножеством списка по всем: иначе
    /// фильтр что-то добавляет, а не отсекает. Флаг описания на состав списка
    /// не влияет — он меняет только вид строки, и то в перегрузке без
    /// платформы.
    /// </summary>
    [Fact]
    public async Task SinglePlatformListIsSubsetOfAllPlatforms()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);

        var all = executor.GetUserCommandsInfo(
            Platform.All,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var twitch = executor.GetUserCommandsInfo(
            Platform.Twitch,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var discord = executor.GetUserCommandsInfo(
            Platform.Discord,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Subset(new HashSet<BaseCommand>(all), new HashSet<BaseCommand>(twitch));
        Assert.Subset(new HashSet<BaseCommand>(all), new HashSet<BaseCommand>(discord));
        Assert.Equal(
            executor
                .GetAdminCommandsInfo(
                    isAddDescription: false,
                    TestContext.Current.CancellationToken
                )
                .Length,
            executor
                .GetAdminCommandsInfo(cancellationToken: TestContext.Current.CancellationToken)
                .Length
        );
    }

    /// <summary>
    /// Разбор параметров объявлен как метод интерфейса со значением по
    /// умолчанию: он нужен всем реализациям <see cref="ICommandService"/>, и
    /// потерять его молча нельзя — вызов перестал бы компилироваться, но
    /// перестал бы и разбирать ввод.
    /// </summary>
    [Fact]
    public async Task ParameterParsingIsAvailableOnTheInterface()
    {
        var executor = await StartedExecutorAsync(TestContext.Current.CancellationToken);
        ICommandService service = executor;
        var info = new[]
        {
            new CommandParameterInfo { Name = "первое", Type = CommandParameterType.String },
            new CommandParameterInfo { Name = "остальное", Type = CommandParameterType.String },
        };

        var parsed = service.ParseParameters("7 \"и ещё\"", info);

        Assert.Equal("7", parsed["первое"]);
        Assert.Equal("и ещё", parsed["остальное"]);
        Assert.Empty(service.ParseParameters(string.Empty, info));
        Assert.Empty(service.ParseParameters("любой ввод", null));
    }

    private static string[] Signature(CommandParameterInfo[] parameters) =>
        [
            .. parameters.Select(parameter =>
                $"{parameter.Name}:{parameter.Type}:{parameter.Required}"
            ),
        ];

    private static BaseCommand CommandWithParametersAndAlias(CommandExecutorService executor) =>
        executor
            .GetUserCommandsInfo(cancellationToken: TestContext.Current.CancellationToken)
            .Concat(
                executor.GetAdminCommandsInfo(
                    cancellationToken: TestContext.Current.CancellationToken
                )
            )
            .First(command => command.Aliases.Length > 0 && command.GetParameterInfo().Length > 0);

    private static async Task<CommandExecutorService> StartedExecutorAsync(
        CancellationToken cancellationToken
    )
    {
        var commandTypes = typeof(BaseCommand)
            .Assembly.GetTypes()
            .Where(type =>
                typeof(BaseCommand).IsAssignableFrom(type)
                && !type.IsAbstract
                && type != typeof(BaseCommand)
                && !type.IsGenericTypeDefinition
            );

        await using var provider = DependencyStubFactory.Build(commandTypes);
        var executor = new CommandExecutorService(
            new CommandFactory(provider, NullLogger<CommandFactory>.Instance)
        );

        await executor.StartAsync(cancellationToken);
        return executor;
    }
}
