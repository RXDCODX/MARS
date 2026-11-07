using MARS.MediaStorage.Services.Storage;
using MARS.MediaStorage.Services.Telegram;
using MARS.Shared.Contracts.Telegram;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Отправка сообщений администраторам через Telegram-бота.
///
/// <c>SendMessage</c> в Telegram.Bot 22 — расширяющий метод, поэтому проверяется сам
/// запрос клиента: текст должен уйти с HTML-разметкой, иначе уведомление пришло бы
/// пользователю с тегами.
/// </summary>
public class TelegramAdminMessengerTests
{
    [Fact]
    public async Task MessageIsSentAsHtml()
    {
        var bot = new Mock<ITelegramBotClient>();

        await new TelegramAdminMessenger(bot.Object).SendAsync(
            42,
            "<b>готово</b>",
            TestContext.Current.CancellationToken
        );

        var requests = bot
            .Invocations.Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<SendMessageRequest>()
            .ToArray();

        var request = Assert.Single(requests);
        Assert.Equal(42, request.ChatId.Identifier);
        Assert.Equal("<b>готово</b>", request.Text);
        Assert.Equal(ParseMode.Html, request.ParseMode);
    }
}
