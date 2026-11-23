using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._6_RussianRoulette;

/// <summary>
/// Обработчик reward-события <c>RewardRussianRoulette</c>.
/// </summary>
public class RussianRouletteHandler : RewardHandlerBase
{
    private readonly ITwitchEventValidationService _validator;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IRewardEventManager _rewardEventManager;

    private readonly ILogger<RussianRouletteHandler> _logger;
    private readonly IWaifuGachaClient _waifuGachaClient;
    private readonly ITwitchInternalClient _twitchClient;

    private readonly Lock _stateLock = new();
    private bool _isGameRunning;
    private readonly List<string> _players = [];
    private readonly Dictionary<string, string> _playerNames = [];

    public RussianRouletteHandler(
        ILogger<RussianRouletteHandler> logger,
        IWaifuGachaClient waifuGachaClient,
        ITwitchInternalClient twitchClient,
        ITwitchEventValidationService validator,
        IHostApplicationLifetime lifetime,
        IRewardEventManager rewardEventManager
    )
    {
        _validator = validator;
        _lifetime = lifetime;
        _rewardEventManager = rewardEventManager;

        _logger = logger;
        _waifuGachaClient = waifuGachaClient;
        _twitchClient = twitchClient;
    }

    public override string RewardCode => "russian_roulette";
    public override int RewardCost { get; } = 6;
    public override string AlertDisplayName { get; set; } = "🔫 Русская рулетка";
    public override string AlertDescription { get; set; } =
        "🎰 Есть вариант игр на 1 игрока, на 2 игров и на множество до 8";
    public override Color Color { get; set; } = Color.FromArgb(247, 0, 255);
    public override int Cost { get; init; } = 6;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    private Task HandleChatAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        lock (_stateLock)
        {
            if (!_isGameRunning)
            {
                return Task.CompletedTask;
            }

            if (
                message.Message.Trim().Equals("участвовать", StringComparison.OrdinalIgnoreCase)
                || message.Message.Trim().Equals("join", StringComparison.OrdinalIgnoreCase)
            )
            {
                if (!_players.Contains(message.UserId))
                {
                    _players.Add(message.UserId);
                    _playerNames[message.UserId] = message.DisplayName ?? message.UserName;
                }
            }
        }

        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            _lifetime,
            _rewardEventManager,
            onRedeem: ValidatedRedeem(_validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled),
            onChat: HandleChatAsync
        );
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        lock (_stateLock)
        {
            if (_isGameRunning)
            {
                return;
            }

            _isGameRunning = true;
            _players.Clear();
            _playerNames.Clear();
        }

        await _twitchClient.SendChatMessageAsync(
            new TwitchChatSendRequest
            {
                Message = "🔫 Русская рулетка запущена! Пишите «участвовать» в течение 30 секунд.",
            }
        );

        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        List<string> players;
        lock (_stateLock)
        {
            players = [.. _players];
        }

        if (players.Count == 0)
        {
            lock (_stateLock)
            {
                _isGameRunning = false;
            }
            await _twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest { Message = "Никто не вступил. Игра отменена." }
            );
            return;
        }

        if (players.Count == 1)
        {
            var solo = players[0];
            await _twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest
                {
                    Message =
                        $"Только один игрок: {_playerNames[solo]}. Обратный отсчёт: 3... 2... 1...",
                }
            );
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            await _twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest
                {
                    Message = $"💥 {_playerNames[solo]} выбыл! (таймаут 10 мин)",
                }
            );
            await _twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest { Message = $"/timeout {_playerNames[solo]} 600" }
            );

            lock (_stateLock)
            {
                _isGameRunning = false;
            }
            return;
        }

        var alive = new List<string>(players);
        while (alive.Count > 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            var loserIndex = Random.Shared.Next(alive.Count);
            var loser = alive[loserIndex];

            var waifuName = await _waifuGachaClient.GetWaifuNameForUserAsync(loser, ct);
            if (waifuName is not null && Random.Shared.NextDouble() < 0.4)
            {
                await _twitchClient.SendChatMessageAsync(
                    new TwitchChatSendRequest
                    {
                        Message = $"💖 {_playerNames[loser]}, твой супруг {waifuName} спас тебя!",
                    }
                );
                continue;
            }

            alive.RemoveAt(loserIndex);
            await _twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest { Message = $"💥 {_playerNames[loser]} выбыл!" }
            );
        }

        var winner = alive[0];
        await _twitchClient.SendChatMessageAsync(
            new TwitchChatSendRequest { Message = $"🏆 Победитель: {_playerNames[winner]}!" }
        );

        await _twitchClient.RecordMiniGameResultAsync(
            new MiniGameResultRequest
            {
                Game = "russian_roulette",
                UserId = winner,
                UserName = _playerNames[winner],
                WithWaifu = false,
            }
        );

        lock (_stateLock)
        {
            _isGameRunning = false;
        }

        _logger.LogInformation("RussianRoulette finished, winner {Winner}", winner);
    }
}
