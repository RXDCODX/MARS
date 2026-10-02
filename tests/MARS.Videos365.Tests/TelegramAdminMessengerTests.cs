using MARS.Shared.Telegram;
using MARS.Videos365.Services;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Videos365.Tests;

/// <summary>
/// Отправка сообщений администраторам в конвейере 365.
///
/// Администраторы узнают о недоступности источника отсюда. Проверяется, что текст
/// уходит с HTML-разметкой: без неё ссылка и заголовок пришли бы как символы.
/// </summary>
public class TelegramAdminMessengerTests
{
    [Fact]
    public async Task MessageIsSentAsHtml()
    {
        var bot = new Mock<ITelegramBotClient>();

        await new TelegramAdminMessenger(bot.Object).SendAsync(
            42,
            "<b>Сайт недоступен</b>",
            TestContext.Current.CancellationToken
        );

        var request = Assert.IsType<SendMessageRequest>(
            Assert.Single(bot.Invocations).Arguments.First()
        );

        Assert.Equal(42, request.ChatId.Identifier);
        Assert.Equal("<b>Сайт недоступен</b>", request.Text);
        Assert.Equal(ParseMode.Html, request.ParseMode);
    }
}
