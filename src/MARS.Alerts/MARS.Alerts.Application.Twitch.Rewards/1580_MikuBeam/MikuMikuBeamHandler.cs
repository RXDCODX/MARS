using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик награды MIKU MIKU BEAM: собирает уникальных зрителей чата за окно
/// активности и отправляет их во фронтенд.
/// </summary>
public class MikuMikuBeamHandler : RewardHandlerBase
{
    private readonly ILogger<MikuMikuBeamHandler> logger;
    private readonly RickRollerService rickRollerService;
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster;

    private readonly Lock _stateLock = new();
    private readonly LinkedList<string> _recentUserIds = [];
    private readonly Dictionary<string, string> _userColors = [];
    private readonly HashSet<string> _knownUserIds = [];
    private readonly HashSet<string> _moderatorIds = [];
    private DateTime _lastActivation = DateTime.MinValue;

    private const int MaxStoredMessages = 100;
    private const int CooldownSeconds = 60;

    public MikuMikuBeamHandler(
        ILogger<MikuMikuBeamHandler> logger,
        RickRollerService rickRollerService,
        EventBroadcaster<TelegramusEvent> broadcaster
    )
    {
        this.logger = logger;
        this.rickRollerService = rickRollerService;
        this._broadcaster = broadcaster;

        OnTwitchMessage += HandleTwitchMessageAsync;
    }

    public override string RewardCode => "miku_miku_beam";

    public override string? CommandKey => "mikubeam";

    public override async Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload is not MikubeamAlertCommandPayload)
        {
            return "Неверный тип параметров для mikubeam.";
        }

        try
        {
            return await ManualActivateAsync();
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка при активации MIKU MIKU BEAM: {ex.Message}";
        }
    }

    public override IRewardActivationPolicy Activation =>
        new CooldownPolicy(TimeSpan.FromSeconds(60));

    /// <summary>Подписчик обычных сообщений: добавляет автора в окно активности.</summary>
    private Task HandleTwitchMessageAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        TrackChatUser(message);
        return Task.CompletedTask;
    }

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
            _userColors[chatEventMessage.UserId] = string.IsNullOrWhiteSpace(
                chatEventMessage.ChatColor
            )
                ? "#FFFFFF"
                : chatEventMessage.ChatColor;

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
                _userColors.Remove(oldest);
            }
        }
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
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
        var uniqueColors = uniqueUserIds
            .Select(id => _userColors.TryGetValue(id, out var c) ? c : "#FFFFFF")
            .ToList();

        logger.LogInformation(
            "MIKU MIKU BEAM activated by {UserName}: {UsersCount} unique users",
            userName,
            uniqueUserIds.Count
        );

        lock (_stateLock)
        {
            _lastActivation = DateTime.Now;
        }

        var beamEvent = new MikuMikuBeamEvent();
        for (var i = 0; i < uniqueUserIds.Count; i++)
        {
            var id = uniqueUserIds[i];
            var color =
                i < _userColors.Count && _userColors.TryGetValue(id, out var c) ? c : "#FFFFFF";
            beamEvent.UsersJson.Add(
                JsonSerializer.Serialize(new { twitchId = id, chatColor = color })
            );
        }

        await _broadcaster.BroadcastAsync(new TelegramusEvent { MikuMikuBeam = beamEvent });
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

        var beamEvent = new MikuMikuBeamEvent();
        for (var i = 0; i < uniqueUserIds.Count; i++)
        {
            var id = uniqueUserIds[i];
            var color =
                i < _userColors.Count && _userColors.TryGetValue(id, out var c) ? c : "#FFFFFF";
            beamEvent.UsersJson.Add(
                JsonSerializer.Serialize(new { twitchId = id, chatColor = color })
            );
        }

        await _broadcaster.BroadcastAsync(new TelegramusEvent { MikuMikuBeam = beamEvent });

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
