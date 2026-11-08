using MARS.Alerts.Twitch.Rewards.Application.Models;
using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик награды MIKU MIKU BEAM: собирает уникальных зрителей чата за окно
/// активности и отправляет их во фронтенд.
/// </summary>
public class MikuMikuBeamHandler(
    ITelegramusNotifier notifier,
    ILogger<MikuMikuBeamHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler, IChatUserTrackingHandler
{
    private readonly Lock _stateLock = new();
    private readonly LinkedList<string> _recentUserIds = [];
    private readonly HashSet<string> _knownUserIds = [];
    private readonly HashSet<string> _moderatorIds = [];
    private DateTime _lastActivation = DateTime.MinValue;

    private const int MaxStoredMessages = 100;
    private const int CooldownSeconds = 60;

    public string RewardKey => "mikumikubeam";

    /// <summary>
    /// Регистрирует участника чата. При переполнении вытесняется самый старый
    /// (первый в <see cref="_recentUserIds"/>), а не только что добавленный,
    /// и из <see cref="_moderatorIds"/> он тоже удаляется.
    /// </summary>
    public void TrackChatUser(ChatEventMessageEvent chatEventMessage)
    {
        lock (_stateLock)
        {
            if (!_knownUserIds.Add(chatEventMessage.UserId))
            {
                return;
            }

            _recentUserIds.AddLast(chatEventMessage.UserId);

            if (chatEventMessage.IsModerator)
            {
                _moderatorIds.Add(chatEventMessage.UserId);
            }

            while (_recentUserIds.Count > MaxStoredMessages)
            {
                var oldest = _recentUserIds.First!.Value;
                _recentUserIds.RemoveFirst();
                _knownUserIds.Remove(oldest);
                _moderatorIds.Remove(oldest);
            }
        }
    }

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => ActivateBeamAsync(rewardEvent.UserName)
            );
        }
        else
        {
            await ActivateBeamAsync(rewardEvent.UserName);
        }
    }

    private async Task ActivateBeamAsync(string userName)
    {
        var remainingCooldown = GetRemainingCooldownSeconds();

        if (remainingCooldown > TimeSpan.Zero)
        {
            logger.LogInformation(
                "MIKU MIKU BEAM: cooldown active, {Seconds} seconds remaining",
                remainingCooldown.TotalSeconds
            );
            return;
        }

        var uniqueUserIds = SnapshotUserIds();

        logger.LogInformation(
            "MIKU MIKU BEAM activated by {UserName}: {UsersCount} unique users",
            userName,
            uniqueUserIds.Count
        );

        lock (_stateLock)
        {
            _lastActivation = DateTime.Now;
        }

        var users = uniqueUserIds.Select(id => (object)new { TwitchId = id }).ToList();
        await notifier.MikuMikuBeam(users);
    }

    public async Task<string> ManualActivateAsync()
    {
        var uniqueUserIds = SnapshotUserIds();

        logger.LogInformation(
            "MIKU MIKU BEAM: manual activation, {UsersCount} users",
            uniqueUserIds.Count
        );

        lock (_stateLock)
        {
            _lastActivation = DateTime.Now;
        }

        var users = uniqueUserIds.Select(id => (object)new { TwitchId = id }).ToList();
        await notifier.MikuMikuBeam(users);

        return $"MIKU MIKU BEAM activated! Participants: {uniqueUserIds.Count}";
    }

    private TimeSpan GetRemainingCooldownSeconds()
    {
        lock (_stateLock)
        {
            var remaining =
                TimeSpan.FromSeconds(CooldownSeconds) - (DateTime.Now - _lastActivation);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private List<string> SnapshotUserIds()
    {
        lock (_stateLock)
        {
            return [.. _recentUserIds];
        }
    }
}
