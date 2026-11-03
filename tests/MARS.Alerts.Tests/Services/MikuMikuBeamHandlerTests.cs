using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Награда MIKU MIKU BEAM.
///
/// Проверяются окно наблюдения и пауза между активациями. Обе вещи видны зрителю:
/// без паузы награда срабатывала бы на каждый клик и заслоняла экран, а с лишними
/// участниками в луче вругались бы люди, которых в чате уже не было.
/// </summary>
public class MikuMikuBeamHandlerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly List<List<object>> _beams = [];
    private readonly MikuMikuBeamHandler _handler;

    public MikuMikuBeamHandlerTests()
    {
        _notifier
            .Setup(instance => instance.MikuMikuBeam(It.IsAny<List<object>>()))
            .Callback<List<object>>(users => _beams.Add(users));
        _handler = new MikuMikuBeamHandler(
            _notifier.Object,
            NullLogger<MikuMikuBeamHandler>.Instance,
            null!
        );
    }

    /// <summary>
    /// В луч попадают только уникальные участники окна: повторное сообщение от
    /// одного человека не должно занимать место другого.
    /// </summary>
    [Fact]
    public async Task OnlyUniqueChatUsersAreCollected()
    {
        Track("1");
        Track("1");
        Track("2");

        await ActivateAsync();

        Assert.Equal(2, BeamSize());
    }

    /// <summary>
    /// Повторная активация в пределах паузы игнорируется: иначе каждый клик по
    /// награде перезапускал бы луч.
    /// </summary>
    [Fact]
    public async Task SecondActivationWaitsForCooldown()
    {
        Track("1");

        await ActivateAsync();
        await ActivateAsync();

        Assert.Single(_beams);
    }

    /// <summary>
    /// Ручная активация паузу не спрашивает: это команда оператора, и она должна
    /// сработать.
    /// </summary>
    [Fact]
    public async Task ManualActivationIgnoresCooldown()
    {
        Track("1");
        await ActivateAsync();

        var message = await _handler.ManualActivateAsync();

        Assert.Equal(2, _beams.Count);
        Assert.Contains("Participants: 1", message);
    }

    /// <summary>
    /// Окно ограничено: самые ранние участники вытесняются, иначе память росла бы
    /// весь стрим.
    /// </summary>
    [Fact]
    public async Task OldestUsersAreDroppedWhenWindowIsFull()
    {
        for (var index = 1; index <= 101; index++)
        {
            Track(index.ToString());
        }

        await ActivateAsync();

        Assert.Equal(100, BeamSize());
    }

    /// <summary>
    /// Окно наблюдения остаётся между активациями: награда не должна забывать
    /// участников только потому, что луч уже показывали.
    /// </summary>
    [Fact]
    public async Task WindowSurvivesCooldown()
    {
        Track("1");
        await ActivateAsync();

        Track("2");
        await ActivateAsync();

        Assert.Single(_beams);
        Assert.Equal(1, BeamSize());

        var message = await _handler.ManualActivateAsync();

        Assert.Contains("Participants: 2", message);
    }

    /// <summary>
    /// Маршрут награды один: обработчик читается по ключу из конфигурации очереди.
    /// </summary>
    [Fact]
    public void RoutingKeyIsBeamReward()
    {
        Assert.Equal(RabbitMqConfig.RewardMikuMikuBeam, _handler.RoutingKey);
    }

    private Task ActivateAsync() =>
        _handler.HandleAsync(Redeemed(), TestContext.Current.CancellationToken);

    private int BeamSize() => _beams[^1].Count;

    private void Track(string userId) =>
        _handler.TrackChatUser(
            new ChatMessageEvent
            {
                UserId = userId,
                UserName = $"user-{userId}",
                IsModerator = false,
            }
        );

    /// <summary>
    /// Событие без пользователя идёт в обход RickRoller: подменять его в тесте
    /// незачем, ветка без него и есть проверяемая.
    /// </summary>
    private static RewardRedeemedEvent Redeemed() =>
        new()
        {
            UserId = "123456789",
            UserName = "Pyro",
            RewardTitle = "MIKU MIKU BEAM",
        };
}
