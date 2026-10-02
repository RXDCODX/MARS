using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities.Subs;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Rewards;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Русская рулетка в чате.
///
/// Проверяются края партии: одиночная игра (обычный розыгрыш без соперников),
/// объявление состава и объявление победителя. Проверяется именно то, что сервис
/// пишет в чат: зритель видит только сообщения, и неверное объявление победителя
/// означало бы, что игра сообщает не то, что произошло.
///
/// Ожидания в игре настоящие (секунды между выстрелами), поэтому проверяются редкие
/// ветки, а не каждая.
/// </summary>
public class RouleteGameTests
{
    [Fact]
    public async Task SinglePlayerGetsPersonalDraw()
    {
        var chat = new ChatRecorder();
        var game = Create([Player("Pyro")], GameType.Normal, chat, Token);

        await game.RussianRoulette();

        Assert.False(game.IsGameRunning);
        Assert.Contains(chat.Messages, message => message.Contains("@Pyro"));
    }

    /// <summary>
    /// Игра на двоих объявляет состав участников: иначе зрители не понимают, о ком
    /// речь.
    /// </summary>
    [Fact]
    public async Task TwoPlayersGetRosterAnnouncement()
    {
        var chat = new ChatRecorder();
        var game = Create([Player("Pyro"), Player("Аяка")], GameType.Normal, chat, Token);

        await game.RussianRoulette();

        Assert.Contains(chat.Messages, message => message.Contains("Играют: Pyro, Аяка"));
        Assert.Contains(chat.Messages, message => message.Contains("Поздравляем"));
    }

    /// <summary>
    /// Отмена останавливает партию: раз выключен сервис, победителя объявлять
    /// некому.
    /// </summary>
    [Fact]
    public async Task CancelledTokenStopsTheGame()
    {
        var chat = new ChatRecorder();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var game = Create([Player("Pyro"), Player("Аяка")], GameType.Normal, chat, cts.Token);

        await game.RussianRoulette();

        Assert.DoesNotContain(chat.Messages, message => message.Contains("Победитель"));
        Assert.DoesNotContain(chat.Messages, message => message.Contains("Поздравляем"));
    }

    /// <summary>
    /// Партия заканчивается объявлением победителя: игрок, выживший в рулетке,
    /// поздравляется в чате.
    /// </summary>
    [Fact]
    public async Task WinnerIsAnnouncedWhenNobodyIsRescued()
    {
        var chat = new ChatRecorder();
        var game = Create([Player("Pyro"), Player("Аяка")], GameType.Normal, chat, Token);

        await game.RussianRoulette();

        Assert.False(game.IsGameRunning);
        Assert.Contains(chat.Messages, message => message.Contains("Поздравляем"));
    }

    private static RouletePlayer Player(string name) =>
        new()
        {
            Name = name,
            TwitchId = Guid.NewGuid().ToString(),
            IsAlive = true,
        };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static RouleteGame Create(
        List<RouletePlayer> players,
        GameType type,
        ChatRecorder chat,
        CancellationToken token,
        IWaifuLookupService? waifu = null
    ) =>
        new(
            players,
            type,
            chat.Client,
            NullLogger.Instance,
            new TwitchTestDbContextFactory(),
            waifu ?? Mock.Of<IWaifuLookupService>(),
            token
        );

    /// <summary>
    /// Клиент Twitch, который только запоминает написанное: всё, что видит зритель
    /// в чате, приходит отсюда.
    /// </summary>
    private sealed class ChatRecorder
    {
        public List<string> Messages { get; } = [];

        public ITwitchClient Client { get; }

        public ChatRecorder()
        {
            var mock = new Mock<ITwitchClient>();
            mock.Setup(instance =>
                    instance.SendMessageAsync(
                        It.IsAny<JoinedChannel>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()
                    )
                )
                .Callback<JoinedChannel, string, bool>((_, message, _) => Messages.Add(message));
            mock.SetupGet(instance => instance.JoinedChannels)
                .Returns([new JoinedChannel(TwitchConstants.Channel)]);
            mock.Setup(instance => instance.GetJoinedChannel(It.IsAny<string>()))
                .Returns(new JoinedChannel(TwitchConstants.Channel));
            Client = mock.Object;
        }
    }
}
