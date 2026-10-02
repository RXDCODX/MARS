using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Services.Management;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Уведомление стримера о переавторизации Twitch.
///
/// При смене токена сервис рассылает администраторам ссылку на авторизацию. Если
/// она не дойдёт, стрим молча останется без событий, поэтому проверяются и
/// адресаты, и содержимое ссылки.
/// </summary>
public class TelegramTokenNotificationTests
{
    [Fact]
    public async Task AdminReceivesAuthorizationLink()
    {
        var messages = new List<(ChatId Id, string Text)>();
        var service = Create(CreateClient(messages), [4242]);

        await service.NotifyStreamerAboutAuthAsync(CreateApi());

        var message = Assert.Single(messages);
        Assert.Equal(4242L, message.Id.Identifier);
        Assert.Contains("id.twitch.tv/oauth2/authorize", message.Text);
        Assert.Contains("client_id=client-id", message.Text);
        Assert.Contains(
            "redirect_uri=http://localhost:8080/TwitchAuth/TwitchUserAuth",
            message.Text
        );
        Assert.Contains("chat:read", message.Text);
    }

    /// <summary>
    /// Сообщение получает каждый администратор: иначе часть из них не смогла бы
    /// переавторизоваться.
    /// </summary>
    [Fact]
    public async Task EveryAdminIsNotified()
    {
        var messages = new List<(ChatId Id, string Text)>();
        var service = Create(CreateClient(messages), [4242, 5252]);

        await service.NotifyStreamerAboutAuthAsync(CreateApi());

        Assert.Equal([4242L, 5252L], messages.Select(message => message.Id.Identifier).Order());
    }

    /// <summary>
    /// Адрес сервера не задан — метод ждёт его, а не рассылает ссылку в никуда.
    /// </summary>
    [Fact]
    public async Task MissingServerAddressIsNotSent()
    {
        var messages = new List<(ChatId Id, string Text)>();
        var service = Create(CreateClient(messages), [4242], withAddress: false);

        await service.NotifyStreamerAboutAuthAsync(CreateApi());

        Assert.Empty(messages);
    }

    private static ITwitchAPI CreateApi()
    {
        var api = new Mock<ITwitchAPI>();
        api.SetupGet(instance => instance.Settings)
            .Returns(new ApiSettings { ClientId = "client-id" });
        return api.Object;
    }

    /// <summary>
    /// Заглушка перехватывает <c>SendRequest</c>, а не <c>SendMessage</c>: в
    /// Telegram.Bot 22 это расширяющий метод, и на нём Moq спотыкается.
    /// </summary>
    private static ITelegramBotClient CreateClient(List<(ChatId Id, string Text)> messages)
    {
        var client = new Mock<ITelegramBotClient>();
        client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>())
            )
            .Callback<IRequest<Message>, CancellationToken>(
                (request, _) =>
                {
                    var send = (SendMessageRequest)request;
                    messages.Add((send.ChatId, send.Text));
                }
            )
            .ReturnsAsync(new Message { Id = 1 });
        return client.Object;
    }

    private static TelegramTokenNotification Create(
        ITelegramBotClient client,
        long[] admins,
        bool withAddress = true
    )
    {
        var server = new Mock<IServer>();
        var features = new FeatureCollection();
        if (withAddress)
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("http://localhost:8080");
            features.Set<IServerAddressesFeature>(addresses);
        }

        server.Setup(instance => instance.Features).Returns(features);

        return new TelegramTokenNotification(
            client,
            server.Object,
            NullLogger<TelegramTokenNotification>.Instance,
            Options.Create(new TelegramConfiguration { AdminIdsArray = admins })
        );
    }
}
