using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Commands.Tests.Adapters;

/// <summary>
/// Платформенные адаптеры команд: префиксы, списки, обрезка ответа и право
/// администратора.
///
/// Общая логика живёт в <see cref="PlatformCommandServiceBase{T}"/>, но проверять
/// её на вымышленном наследнике бессмысленно — она отдаётся платформам, и
/// отличаются адаптеры только префиксом, лимитом длины и правилом «кто админ».
/// Поэтому проверка идёт по настоящим адаптерам.
/// </summary>
public sealed class PlatformCommandAdaptersTests
{
    [Fact]
    public void TwitchAdapterUsesExclamationPrefixAndOwnLimit()
    {
        var adapter = new TwitchCommandService(CommandService(["help", "title"]));

        Assert.Equal(Platform.Twitch, adapter.Platform);
        Assert.Equal(['!'], adapter.GetCommandPrefixes());
        Assert.Equal(500, adapter.GetMaxResponseLength());
        Assert.Equal("help title", adapter.TrimCommandPrefix("!help title"));
        Assert.True(adapter.StartsWithCommandPrefix("!help"));
        Assert.False(adapter.StartsWithCommandPrefix("help"));
        Assert.False(adapter.IsUserAdmin("streamer"));
    }

    [Fact]
    public void DiscordAdapterUsesSlashPrefixAndOwnLimit()
    {
        var adapter = new DiscordCommandService(CommandService(["help"]));

        Assert.Equal(Platform.Discord, adapter.Platform);
        Assert.Equal(['/', '!'], adapter.GetCommandPrefixes());
        Assert.Equal(1900, adapter.GetMaxResponseLength());
        Assert.Equal("help", adapter.TrimCommandPrefix("/help"));
        Assert.True(adapter.StartsWithCommandPrefix("!help"));
        Assert.False(adapter.IsUserAdmin(42UL));
    }

    [Fact]
    public void TelegramAdapterKeepsOwnLimit()
    {
        var adapter = new TelegramCommandService(CommandService(["help"]));

        Assert.Equal(Platform.Telegram, adapter.Platform);
        Assert.Equal(['/'], adapter.GetCommandPrefixes());
        Assert.Equal(4096, adapter.GetMaxResponseLength());
        Assert.False(adapter.IsUserAdmin(42L));
        Assert.Equal(["shutdown"], adapter.AdminCommands.ToArray());
    }

    /// <summary>
    /// База адаптера проверяется на наследнике, который ничего не переопределяет:
    /// все три настоящих адаптера задают свой префикс и свой лимит, иначе
    /// собственные реализации базы остались бы непроверенными.
    /// </summary>
    [Fact]
    public void BaseDefaultsApplyWhenAdapterOverridesNothing()
    {
        var adapter = new BareAdapter(CommandService(["help"]));

        Assert.Equal(1000, adapter.GetMaxResponseLength());
        Assert.Equal(['/'], adapter.GetCommandPrefixes());
        Assert.Equal("help", adapter.TrimCommandPrefix("/help"));
        // Пробелы не трогаются: префикса в них нет, а IsNullOrWhiteSpace
        // отсекает такую строку от обрезки специально.
        Assert.Equal("   ", adapter.TrimCommandPrefix("   "));
        Assert.True(adapter.StartsWithCommandPrefix("/help"));
        Assert.False(adapter.StartsWithCommandPrefix("help"));
        Assert.Equal("Доступные команды:\nhelp", adapter.GetCommandsList("caller"));
        Assert.Equal(new string('x', 1000), adapter.ValidateResponse(new string('x', 1000)));
        Assert.Equal(new string('x', 997) + "...", adapter.ValidateResponse(new string('x', 1001)));
    }

    [Fact]
    public void ApiAdapterReportsItsOwnPlatformAndPrefixes()
    {
        var adapter = new ApiCommandService(
            CommandService(["help"]),
            NullLogger<ApiCommandService>.Instance
        );

        Assert.Equal(Platform.Api, adapter.Platform);
        Assert.Equal(['/', '!'], adapter.CommandPrefixes);
    }

    private sealed class BareAdapter(ICommandService commandService)
        : PlatformCommandServiceBase<string>
    {
        public override Platform Platform => Platform.Api;

        public override IEnumerable<string> UserCommands =>
            commandService.GetUserCommands(Platform.Api, false, CancellationToken.None);

        public override IEnumerable<string> AdminCommands =>
            commandService.GetAdminCommands(Platform.Api, false, CancellationToken.None);

        public override Func<string, bool> IsAdmin => _ => false;
    }

    [Fact]
    public void AdaptersReportCommandsFromTheExecutor()
    {
        var service = CommandService(["help", "info"]);

        Assert.Equal(["help", "info"], new DiscordCommandService(service).UserCommands.ToArray());
        Assert.Equal(["help", "info"], new TelegramCommandService(service).UserCommands.ToArray());

        // Twitch дописывает свой префикс: в чате команда набирается как «!help»,
        // и список обязан показывать её в том виде, в каком её вводят.
        Assert.Equal(["!help", "!info"], new TwitchCommandService(service).UserCommands.ToArray());
    }

    [Fact]
    public void AvailabilityIsAnsweredByTheExecutor()
    {
        var service = CommandService(["help"]);

        Assert.True(new TwitchCommandService(service).IsCommandAvailable("help"));
        Assert.True(new DiscordCommandService(service).IsCommandAvailable("help"));
        Assert.True(new TelegramCommandService(service).IsCommandAvailable("help"));
    }

    /// <summary>
    /// Админ-команды скрыты от всех адаптеров: право вызывающего определяет
    /// сервис платформы, и через адаптер его проверить нечем. Список админских
    /// команд поэтому всегда пуст, а <c>IsAdmin</c> — всегда false.
    /// </summary>
    [Fact]
    public void NobodyBecomesAdminThroughAdapter()
    {
        var service = CommandService(["help"]);

        Assert.False(
            new ApiCommandService(service, NullLogger<ApiCommandService>.Instance).IsAdmin("caller")
        );
        Assert.False(new TwitchCommandService(service).IsAdmin("streamer"));
        Assert.False(new DiscordCommandService(service).IsAdmin(1UL));
        Assert.False(new TelegramCommandService(service).IsAdmin(1L));
    }

    /// <summary>
    /// Сами списки админ-команд адаптер отдаёт как пришли: скрывает их
    /// исполнитель, который и знает про платформу. Проверять тут нечего — на
    /// API админских команд нет вовсе, и это уже проверяет тест исполнителя.
    /// </summary>
    [Fact]
    public void AdminListComesFromExecutorAsIs()
    {
        var service = CommandService(["help"], ["shutdown"]);

        Assert.Equal(["shutdown"], new DiscordCommandService(service).AdminCommands.ToArray());
        Assert.Equal(["!shutdown"], new TwitchCommandService(service).AdminCommands.ToArray());
    }

    [Fact]
    public void CommandsListIsBuiltFromExecutor()
    {
        var service = CommandService(["help", "info"]);

        var list = new ApiCommandService(
            service,
            NullLogger<ApiCommandService>.Instance
        ).GetCommandsList("caller", includeAdminCommands: false);

        Assert.Contains("help", list);
        Assert.Contains("info", list);
    }

    [Fact]
    public void AdminListIsRefusedToNonAdmin()
    {
        var service = CommandService(["help"]);

        var list = new ApiCommandService(
            service,
            NullLogger<ApiCommandService>.Instance
        ).GetCommandsList("caller", includeAdminCommands: true);

        Assert.Equal("У вас нет прав для просмотра админских команд.", list);
    }

    [Fact]
    public void EmptyRoleGetsExplicitMessage()
    {
        var service = new Mock<ICommandService>();
        service.Setup(instance => instance.GetUserCommands(It.IsAny<Platform>())).Returns([]);
        service.Setup(instance => instance.GetAdminCommands(It.IsAny<Platform>())).Returns([]);

        var list = new ApiCommandService(
            service.Object,
            NullLogger<ApiCommandService>.Instance
        ).GetCommandsList("caller");

        Assert.Equal("Нет доступных команд для вашей роли.", list);
    }

    [Fact]
    public void ExplicitCommandListsAreSorted()
    {
        var service = CommandService(["help"]);

        var list = new ApiCommandService(
            service,
            NullLogger<ApiCommandService>.Instance
        ).GetCommandsList("caller", ["zebra", "apple"], ["shutdown"]);

        Assert.Contains("apple", list);
        Assert.DoesNotContain("shutdown", list);
    }

    /// <summary>
    /// Обрезка ответа: лимит у каждой платформы свой, а Twitch обрезает ответ
    /// сам и отдаёт адаптеру текст целиком — иначе длинное сообщение ушло бы
    /// двумя кусками там, где об этом не просили.
    /// </summary>
    [Fact]
    public void ResponseIsTrimmedToPlatformLimit()
    {
        var long_ = new string('x', 4000);

        Assert.Equal(long_, new TwitchCommandService(CommandService()).ValidateResponse(long_));
        Assert.Equal(
            1900,
            new DiscordCommandService(CommandService()).ValidateResponse(long_).Length
        );
        Assert.Equal(4096, new TelegramCommandService(CommandService()).GetMaxResponseLength());
    }

    [Fact]
    public void ShortResponseIsNotTouched()
    {
        var service = CommandService();

        Assert.Equal(
            "коротко",
            new ApiCommandService(service, NullLogger<ApiCommandService>.Instance).ValidateResponse(
                "коротко"
            )
        );
    }

    [Fact]
    public void ApiAdapterTruncatesWithItsOwnMarker()
    {
        var adapter = new ApiCommandService(
            CommandService(),
            NullLogger<ApiCommandService>.Instance
        );

        var trimmed = adapter.ValidateResponse(new string('y', 12000));

        Assert.Equal(10000, adapter.GetMaxResponseLength());
        Assert.EndsWith("[Ответ обрезан...]", trimmed);
        Assert.StartsWith(new string('y', 100), trimmed);
    }

    private static ICommandService CommandService(string[]? userCommands = null) =>
        CommandService(userCommands ?? [], userCommands is null ? [] : ["shutdown"]);

    private static ICommandService CommandService(string[] userCommands, string[] adminCommands)
    {
        var service = new Mock<ICommandService>();
        service
            .Setup(instance =>
                instance.GetUserCommands(
                    It.IsAny<Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(userCommands);
        service
            .Setup(instance =>
                instance.GetUserCommands(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns(userCommands);
        service
            .Setup(instance =>
                instance.GetAdminCommands(
                    It.IsAny<Platform>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(adminCommands);
        service
            .Setup(instance =>
                instance.GetAdminCommands(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .Returns(adminCommands);
        service
            .Setup(instance =>
                instance.IsCommandAvailable(It.IsAny<string>(), It.IsAny<Platform>())
            )
            .Returns(true);
        return service.Object;
    }
}
