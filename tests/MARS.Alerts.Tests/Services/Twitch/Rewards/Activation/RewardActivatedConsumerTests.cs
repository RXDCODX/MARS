using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Единственный потребитель активаций наград.
/// </summary>
/// <remarks>
/// Проверяются ровно те решения, которые потребитель принимает до клиента:
/// что отправить, с каким медиа и кому. Рендера здесь нет — его выбирает
/// оверлей по <c>alertKey</c>, поэтому проверка формы события и есть проверка
/// контракта.
/// </remarks>
public class RewardActivatedConsumerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly Mock<IRewardMediaResolver> _resolver = new();

    public RewardActivatedConsumerTests()
    {
        _resolver
            .Setup(resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task ActivationWithoutAlertKeyIsSkipped()
    {
        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: null));

        _resolver.Verify(
            resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    It.IsAny<string>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task EffectRunsAndItsDataIsSent()
    {
        var data = new { Seconds = 60 };
        var effect = new StubEffect(RewardAlertKeys.AdhdSuperpower)
        {
            Result = new RewardEffectResult { Data = data },
        };
        var consumer = CreateConsumer(chance: "0", effect);

        await HandleAsync(
            consumer,
            Event(alertKey: RewardAlertKeys.AdhdSuperpower, userInput: "hello", cost: 350)
        );

        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    RewardAlertKeys.AdhdSuperpower,
                    It.IsAny<object?>(),
                    "hello",
                    350,
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    data
                ),
            Times.Once
        );
        Assert.Equal(1, effect.Calls);
    }

    /// <summary>
    /// Награда без серверного эффекта всё равно активируется: имя, цвет, цена и
    /// ввод зрителя нужны клиенту, даже если рисовать ему нечего.
    /// </summary>
    [Fact]
    public async Task UnknownKeyIsSentWithoutEffect()
    {
        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.Credits));

        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    RewardAlertKeys.Credits,
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    null
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SuppressedEffectDoesNotSendEvent()
    {
        var effect = new StubEffect(RewardAlertKeys.CloseGame)
        {
            Result = RewardEffectResult.Suppressed,
        };
        var consumer = CreateConsumer(chance: "0", effect);

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.CloseGame));

        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    It.IsAny<string>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Медиа из хранилища клонируется и получает имя и цвет зрителя: шаблон
    /// приходит с подстановками <c>{user.name}</c> и <c>{user.color}</c>.
    /// </summary>
    [Fact]
    public async Task MediaFromStorageIsPreparedForViewer()
    {
        var template = Media("{user.name}", "{user.color}");
        _resolver
            .Setup(resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([template]);

        IReadOnlyList<MediaInfo>? sent = null;
        Capture(media => sent = media);

        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(
            consumer,
            Event(
                alertKey: RewardAlertKeys.Credits,
                user: User(color: "#00FF00"),
                userInput: "текст"
            )
        );

        var media = Assert.Single(sent!);
        Assert.NotSame(template, media);
        Assert.Equal("Pyro", media.TextInfo.Text);
        Assert.Equal("#00FF00", media.TextInfo.KeyWordsColor);
        // Имя шаблона не подменяется: DisplayName — подпись файла в хранилище.
        Assert.Equal("alert", media.MetaInfo.DisplayName);
    }

    [Fact]
    public async Task ViewerIsRebuiltWhenEventHasNoUser()
    {
        object? sentUser = null;
        _notifier
            .Setup(notifier =>
                notifier.RewardActivated(
                    It.IsAny<string>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                )
            )
            .Callback<string, object?, string?, int, IReadOnlyList<MediaInfo>, object?>(
                (_, user, _, _, _, _) => sentUser = user
            )
            .Returns(Task.CompletedTask);

        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.Credits, user: null));

        var viewer = Assert.IsType<TwitchUser>(sentUser);
        Assert.Equal("Pyro", viewer.DisplayName);
        Assert.Equal("Pyro", viewer.UserLogin);
        Assert.Equal("42", viewer.TwitchId);
    }

    /// <summary>
    /// Рикролл централизован: он проверяется до эффекта, и при срабатывании
    /// активация не отправляется вовсе. Раньше каждый обработчик проверял его
    /// сам, и награда без проверки тихо перестала роллиться.
    /// </summary>
    [Fact]
    public async Task RickRollReplacesRollableReward()
    {
        var effect = new StubEffect(RewardAlertKeys.Confetti);
        var consumer = CreateConsumer(chance: "1", effect);

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.Confetti, user: User()));

        _notifier.Verify(notifier => notifier.Alert(It.IsAny<MediaDto>()), Times.Once);
        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    It.IsAny<string>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                ),
            Times.Never
        );
        Assert.Equal(0, effect.Calls);
    }

    /// <summary>
    /// Возврат баллов и закрытие игры роллить нельзя: иначе зритель заплатил бы
    /// за возврат и получил рикролл.
    /// </summary>
    [Fact]
    public async Task RickRollIsNotAppliedToNonRollableReward()
    {
        var consumer = CreateConsumer(chance: "1");

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.AllRefund));

        _notifier.Verify(notifier => notifier.Alert(It.IsAny<MediaDto>()), Times.Never);
        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    RewardAlertKeys.AllRefund,
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task RickRollRequiresKnownViewer()
    {
        var consumer = CreateConsumer(chance: "1");

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.Confetti, user: null));

        _notifier.Verify(notifier => notifier.Alert(It.IsAny<MediaDto>()), Times.Never);
        _notifier.Verify(
            notifier =>
                notifier.RewardActivated(
                    RewardAlertKeys.Confetti,
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Тело без полей — не «пустая награда», а битое сообщение: оно обязано
    /// уйти в ретрай и DLQ, иначе потеряется молча.
    /// </summary>
    [Fact]
    public async Task UnreadableBodyThrows()
    {
        var consumer = CreateConsumer(chance: "0");

        // Вызов идёт через рефлексию: `HandleMessageAsync` защищённый, а сборка
        // не открывает внутренности тестам. Синхронная обёртка `Invoke` при
        // асинхронном методе исключение не оборачивает, поэтому разворачиваем
        // оба случая — и обёртку, и исходное исключение из задачи.
        var exception = await Record.ExceptionAsync(() => HandleAsync(consumer, "null"));
        var inner = exception is TargetInvocationException invocation
            ? invocation.InnerException
            : exception;

        Assert.IsType<InvalidOperationException>(inner);
    }

    /// <summary>
    /// Два эффекта на один ключ — ошибка регистрации, но падать нельзя:
    /// побеждает первый, в журнал уходит предупреждение.
    /// </summary>
    [Fact]
    public void DuplicateEffectsKeepFirst()
    {
        var first = new StubEffect(RewardAlertKeys.Confetti);
        var second = new StubEffect(RewardAlertKeys.Confetti);

        var map = RewardActivatedConsumer.BuildEffectMap([first, second], NullLogger.Instance);

        Assert.Same(first, map[RewardAlertKeys.Confetti]);
    }

    private void Capture(Action<IReadOnlyList<MediaInfo>> capture) =>
        _notifier
            .Setup(notifier =>
                notifier.RewardActivated(
                    It.IsAny<string>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<MediaInfo>>(),
                    It.IsAny<object?>()
                )
            )
            .Callback<string, object?, string?, int, IReadOnlyList<MediaInfo>, object?>(
                (_, _, _, _, media, _) => capture(media)
            )
            .Returns(Task.CompletedTask);

    private RewardActivatedConsumer CreateConsumer(
        string chance,
        params IRewardActivationEffect[] effects
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = chance }
            )
            .Build();

        return new RewardActivatedConsumer(
            Options.Create(new RabbitMqOptions()),
            effects,
            _resolver.Object,
            new RickRollerService(_notifier.Object, configuration),
            _notifier.Object,
            NullLogger<RewardActivatedConsumer>.Instance
        );
    }

    private static Task HandleAsync(RewardActivatedConsumer consumer, string json)
    {
        var method = typeof(RewardActivatedConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)
            method.Invoke(
                consumer,
                [RabbitMqConfig.RewardActivated, json, TestContext.Current.CancellationToken]
            )!;
    }

    private static string Event(
        string? alertKey,
        string? userInput = null,
        int cost = 100,
        TwitchUser? user = null
    ) =>
        JsonSerializer.Serialize(
            new RewardRedeemedEvent
            {
                RewardId = Guid.NewGuid().ToString(),
                RewardTitle = "название больше не участвует в маршруте",
                AlertKey = alertKey,
                UserId = "42",
                UserName = "Pyro",
                UserInput = userInput,
                Cost = cost,
                MessageId = "redemption-1",
                User = user,
            }
        );

    private static TwitchUser User(string? color = null) =>
        new()
        {
            TwitchId = "42",
            UserLogin = "pyro",
            DisplayName = "Pyro",
            ChatColor = color,
        };

    private static MediaInfo Media(string? text = null, string? color = null) =>
        new()
        {
            TextInfo = new MediaTextInfo { Text = text, KeyWordsColor = color },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alerts/alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "alert" },
            StylesInfo = new MediaStylesInfo(),
        };

    private sealed class StubEffect(string alertKey) : IRewardActivationEffect
    {
        public string AlertKey { get; } = alertKey;

        public RewardEffectResult Result { get; set; } = RewardEffectResult.Passthrough;

        public int Calls { get; private set; }

        public Task<RewardEffectResult> ApplyAsync(
            RewardActivationContext context,
            CancellationToken cancellationToken
        )
        {
            Calls++;

            return Task.FromResult(Result);
        }
    }
}
