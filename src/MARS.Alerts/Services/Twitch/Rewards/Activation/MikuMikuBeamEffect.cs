using MARS.Shared.Messaging;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда MIKU MIKU BEAM: собирает уникальных зрителей чата за окно активности
/// и передаёт их в данных активации.
/// </summary>
/// <remarks>
/// <para>
/// Эффект остаётся и <see cref="IChatUserTrackingHandler"/>: без трекера чата
/// участников взять неоткуда, а отдельный трекер дублировал бы окно и его
/// вытеснение. Регистрируется один экземпляр на два интерфейса.
/// </para>
/// <para>
/// Пауза между активациями своя, 60 секунд. Это не лимит награды: баллы при
/// повторной активации не возвращаются, как и в монолите, — просто луч не
/// перезапускается, пока предыдущий ещё идёт.
/// </para>
/// </remarks>
public sealed class MikuMikuBeamEffect(
    TimeProvider timeProvider,
    ILogger<MikuMikuBeamEffect> logger
) : IChatUserTrackingHandler, IRewardActivationEffect
{
    private const int MaxStoredMessages = 100;
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    private readonly Lock _stateLock = new();
    private readonly LinkedList<string> _recentUserIds = [];
    private readonly HashSet<string> _knownUserIds = [];
    private DateTimeOffset _lastActivation = DateTimeOffset.MinValue;

    public string AlertKey => RewardAlertKeys.MikuMikuBeam;

    /// <summary>
    /// Регистрирует участника чата. При переполнении вытесняется самый старый,
    /// а не только что добавленный: иначе окно всегда состояло бы из последнего
    /// зрителя.
    /// </summary>
    public void TrackChatUser(ChatMessageEvent chatMessage)
    {
        lock (_stateLock)
        {
            if (!_knownUserIds.Add(chatMessage.UserId))
            {
                return;
            }

            _recentUserIds.AddLast(chatMessage.UserId);

            while (_recentUserIds.Count > MaxStoredMessages)
            {
                var oldest = _recentUserIds.First!.Value;
                _recentUserIds.RemoveFirst();
                _knownUserIds.Remove(oldest);
            }
        }
    }

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        var result = RewardEffectResult.Suppressed;

        lock (_stateLock)
        {
            var remaining = Cooldown - (timeProvider.GetLocalNow() - _lastActivation);

            if (_lastActivation != DateTimeOffset.MinValue && remaining > TimeSpan.Zero)
            {
                logger.LogInformation(
                    "MIKU MIKU BEAM: cooldown active, {Seconds} seconds remaining",
                    (int)remaining.TotalSeconds
                );
            }
            else
            {
                _lastActivation = timeProvider.GetLocalNow();
                var users = _recentUserIds.Select(id => (object)new { TwitchId = id }).ToList();

                logger.LogInformation(
                    "MIKU MIKU BEAM activated by {UserName}: {UsersCount} unique users",
                    context.UserName,
                    users.Count
                );

                result = new RewardEffectResult { Data = new { Users = users } };
            }
        }

        return Task.FromResult(result);
    }
}
