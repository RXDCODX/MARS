using Google.Protobuf.Reflection;
using MARS.Shared.Grpc.EventSub;
using MARS.Shared.Grpc.Scoreboard;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.Shared.Grpc.TelegramLog;
using MARS.Shared.Grpc.Telegramus;
using MARS.Shared.Grpc.Tuna;
using CommandsService = MARS.Shared.Grpc.Commands.Commands;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Контракт gRPC фиксируется тестом: имя сервиса и состав его методов — это
/// то, о чём договариваются клиент и сервер, и переименование метода в proto
/// ломает разговор молча, а всплывает на стенде, когда пора чинить.
/// </summary>
/// <remarks>
/// Проверяется сгенерированное описание сервиса, а не сгенерированный клиент:
/// имя метода в коде и имя метода в протоколе — разные строки, и проверить
/// надо обе.
/// </remarks>
public class ServiceDescriptorTests
{
    [Fact]
    public void Контракт_Команд()
    {
        AssertContract(CommandsService.Descriptor, "mars.commands.Commands", "Invoke", "List");
    }

    [Fact]
    public void Контракт_Табло_Счёта()
    {
        AssertContract(
            ScoreboardService.Descriptor,
            "mars.scoreboard.ScoreboardService",
            "Subscribe",
            "GetCurrentState",
            "UpdateState",
            "UpdatePlayerScore",
            "SetPlayerFinal",
            "SetVisibility"
        );
    }

    [Fact]
    public void Контракт_Админки_Telegram()
    {
        AssertContract(
            TelegramAdminService.Descriptor,
            "mars.telegramadmin.TelegramAdminService",
            "SendToAdmins"
        );
    }

    [Fact]
    public void Контракт_Журнала_В_Telegram()
    {
        AssertContract(
            TelegramLogService.Descriptor,
            "mars.telegramlog.TelegramLogService",
            "Publish"
        );
    }

    [Fact]
    public void Контракт_Оверлея()
    {
        AssertContract(
            TelegramusService.Descriptor,
            "mars.telegramus.TelegramusService",
            "Subscribe",
            "LogError",
            "TwitchMsg",
            "Fire",
            "GetAdhdConfig",
            "UpdateAdhdConfig"
        );
    }

    [Fact]
    public void Контракт_Плеера()
    {
        AssertContract(
            TunaService.Descriptor,
            "mars.tuna.TunaService",
            "Subscribe",
            "SendPlayerData",
            "BeYm"
        );
    }

    [Fact]
    public void Контракт_Подписок_EventSub()
    {
        AssertContract(
            EventSubSubscriptionService.Descriptor,
            "mars.eventsub.EventSubSubscriptionService",
            "CreateSubscription",
            "ListSubscriptions",
            "DeleteSubscription"
        );
    }

    private static void AssertContract(
        ServiceDescriptor descriptor,
        string fullName,
        params string[] methods
    )
    {
        Assert.Equal(fullName, descriptor.FullName);
        Assert.Equal(
            methods.OrderBy(name => name, StringComparer.Ordinal),
            descriptor
                .Methods.Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
        );
    }
}
