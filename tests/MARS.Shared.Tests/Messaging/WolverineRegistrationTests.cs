using System.Reflection;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Runtime;
using Wolverine.Util;

namespace MARS.Shared.Tests.Messaging;

/// <summary>
/// Проверки регистрации Wolverine как шины обмена. Настоящий брокер здесь не
/// нужен: без <c>RabbitMq:Host</c> шина обязана не регистрироваться вовсе, а
/// правила обнаружения обработчиков и имена очередей проверяются по
/// <see cref="WolverineOptions"/> до старта хоста.
/// </summary>
/// <remarks>
/// Топология (обменники, очереди, DLQ, биндинги) появляется только при старте
/// хоста, поэтому она проверяется сквозным тестом против Testcontainers в
/// <c>MarsWolverineTopologyTests</c>. Здесь — то, что видно без соединения.
/// </remarks>
public class WolverineRegistrationTests
{
    [Fact]
    public void БезАдресаБрокераШинаНеРегистрируется()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = string.Empty;

        builder.AddMarsWolverine("MARS.Probe", typeof(WolverineRegistrationTests).Assembly);

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IWolverineRuntime)
        );
    }

    [Fact]
    public void САдресомБрокераШинаРегистрируетсяПодИменемСервиса()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine("MARS.Probe", typeof(WolverineRegistrationTests).Assembly);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Equal("MARS.Probe", options.ServiceName);
    }

    [Fact]
    public void ОбработчикомСчитаетсяТолькоТипССуффиксомListener()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine("MARS.Probe", typeof(WolverineRegistrationTests).Assembly);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(ProbeListener)));
        Assert.Contains(
            "MISS -- Name ends with 'Listener'",
            options.DescribeHandlerMatch(typeof(ProbeHandler))
        );
    }

    [Fact]
    public void ПравилоСуффиксаСкладываетсяСПравиламиМетодовWolverine()
    {
        // Отключение встроенного обнаружения снимает только отбор типов, а не
        // правила методов: слушатель с HandleAsync(String, CancellationToken)
        // обязан остаться обработчиком. Иначе шина находила бы класс и теряла
        // его метод.
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine("MARS.Probe", typeof(WolverineRegistrationTests).Assembly);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(ProbeAsyncListener)));
    }

    [Fact]
    public void КаждоеСообщениеШиныИмеетПсевдонимИОбменникРавенПсевдониму()
    {
        Type[] contracts =
        [
            typeof(RewardRedeemedEvent),
            typeof(ChatMessageReceivedEvent),
            typeof(RewardInputMessageEvent),
            typeof(ChatMessageDeletedEvent),
            typeof(ChatSendEvent),
            typeof(EventSubRawEvent),
        ];

        foreach (var contract in contracts)
        {
            var identity = contract.GetCustomAttribute<MessageIdentityAttribute>();

            Assert.NotNull(identity);
            Assert.False(string.IsNullOrWhiteSpace(identity.Alias));
            Assert.StartsWith("mars.", identity.Alias);
            Assert.Equal(identity.Alias, contract.ToMessageTypeName());
        }
    }

    [Fact]
    public void КаждоеСообщениеШиныОбъявленоВОдномМесте()
    {
        // Псевдонимы собраны в одном классе и не дублируются: два разных типа с
        // одинаковым [MessageIdentity] легли бы в один обменник, и потребитель
        // получал бы чужие сообщения.
        var contracts = typeof(MarsMessageAliases)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(contracts);
        Assert.Equal(contracts.Length, contracts.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ОчередьСлушателяВыводитсяИзИмениТипа()
    {
        Assert.Equal(
            "mars.alerts.media",
            MarsQueues.QueueNameForHandlerType(typeof(TwitchMediaListener))
        );
        Assert.Equal(
            "mars.alerts.reward-dispatch",
            MarsQueues.QueueNameForHandlerType(typeof(RewardRedeemedListener))
        );
        Assert.Equal(
            "mars.twitchcore.chat-send",
            MarsQueues.QueueNameForHandlerType(typeof(ChatSendListener))
        );
        Assert.Equal(
            "mars.waifu.roll-cooldown",
            MarsQueues.QueueNameForHandlerType(typeof(RollCooldownListener))
        );
    }

    [Fact]
    public void УОбработчикаБезСуффиксаListenerОчередиНет()
    {
        // Возврат null — это и есть механизм, которым Wolverine отсекает тип от
        // создания слушателя. Опечатка в имени очереди иначе завела бы вторую
        // копию reward-диспетчера на собственном соединении.
        Assert.Null(MarsQueues.QueueNameForHandlerType(typeof(ProbeHandler)));
        Assert.Null(MarsQueues.QueueNameForHandlerType(typeof(ConfettiHandler)));
    }

    [Fact]
    public void ИмяОчередиЗадаётсяЯвноИНеВыводитсяИзПространстваИмён()
    {
        Assert.Equal(
            "mars.alerts.reward-input",
            MarsQueues.QueueNameForHandlerType(typeof(RewardInputMessageListener))
        );
        Assert.Equal(
            "mars.alerts.chat-users",
            MarsQueues.QueueNameForHandlerType(typeof(ChatUserListener))
        );
        Assert.StartsWith("mars.", MarsQueues.DeadLetterQueueForService("MARS.Alerts"));
        Assert.Equal("mars.alerts.dlq", MarsQueues.DeadLetterQueueForService("MARS.Alerts"));
    }

    // Фиктивные типы: проверяют правила обнаружения и имя очереди, к сервисам не
    // относятся. Живут здесь, потому что настоящие слушатели лежат в сборках
    // сервисов, а MARS.Shared на них не ссылается и ссылаться не должен.
    private sealed class ProbeListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ProbeHandler
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ProbeAsyncListener
    {
        public static Task HandleAsync(string message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TwitchMediaListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RewardRedeemedListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ChatSendListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RollCooldownListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RewardInputMessageListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ChatUserListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ConfettiHandler
    {
        public static void Handle(string message) => _ = message;
    }
}
