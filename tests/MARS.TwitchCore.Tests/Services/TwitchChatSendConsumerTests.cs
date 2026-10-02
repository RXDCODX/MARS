using System.Reflection;
using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Services.Chat;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Отправка сообщений в чат по команде из очереди.
///
/// Сервис — единственная точка записи в чат Twitch для остальных микросервисов.
/// Проверяется нормализация переносов и дробление длинного текста: Twitch отклоняет
/// сообщение длиннее 500 символов, и без дробления команда молча не дошла бы до
/// чата.
/// </summary>
public class TwitchChatSendConsumerTests
{
    private readonly Mock<ITwitchClient> _client = new();

    [Fact]
    public async Task MessageIsSentToChannel()
    {
        await HandleMessage("привет");

        Assert.Equal(["привет"], SentMessages());
    }

    /// <summary>
    /// Переносы строк превращаются в пробелы: в чате они разорвали бы строку и
    /// испортили вид сообщения.
    /// </summary>
    [Fact]
    public async Task LineBreaksBecomeSpaces()
    {
        await HandleMessage("первая\r\nвторая");

        Assert.Equal(["первая вторая"], SentMessages());
    }

    /// <summary>
    /// Пустое сообщение игнорируется: в чате появилась бы пустая плашка.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyMessageIsSkipped(string? message)
    {
        await HandleMessage(message!);

        Assert.Empty(SentMessages());
    }

    /// <summary>
    /// Длинное сообщение дробится по границам слов: так текст читается целиком, а не
    /// обрывается на середине.
    /// </summary>
    [Fact]
    public async Task LongMessageIsSplit()
    {
        var longText = string.Join(' ', Enumerable.Repeat("слово", 200));

        await HandleMessage(longText);

        Assert.True(SentMessages().Count > 1);
        Assert.All(SentMessages(), message => Assert.True(message.Length <= 500));
    }

    /// <summary>
    /// Канал из команды уважается: команды шлют сообщения в свой канал, а не в
    /// основной.
    /// </summary>
    [Fact]
    public async Task TargetChannelIsUsed()
    {
        await Handle(
            json: JsonSerializer.Serialize(
                new ChatSendEvent { Message = "привет", Channel = "другой-канал" }
            )
        );

        _client.Verify(
            client => client.SendMessageAsync("другой-канал", It.IsAny<string>(), It.IsAny<bool>()),
            Times.Once
        );
    }

    /// <summary>
    /// Отправка идёт расширяющим методом по имени канала, поэтому из вызова
    /// берётся второй аргумент — сам текст.
    /// </summary>
    private IReadOnlyList<string> SentMessages() =>
        _client
            .Invocations.Select(invocation => invocation.Arguments.OfType<string>().LastOrDefault())
            .OfType<string>()
            .ToArray();

    private Task HandleMessage(string message) =>
        Handle(JsonSerializer.Serialize(new ChatSendEvent { Message = message }));

    private Task Handle(string json)
    {
        var consumer = new TwitchChatSendConsumer(
            Options.Create(new RabbitMqOptions()),
            _client.Object,
            NullLogger<TwitchChatSendConsumer>.Instance
        );
        var method = typeof(TwitchChatSendConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)
            method.Invoke(
                consumer,
                [RabbitMqConfig.ChatSend, json, TestContext.Current.CancellationToken]
            )!;
    }
}
