using MARS.Shared.Clients;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchStream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

namespace MARS.TwitchCore.Services.TekkenStreams;

/// <summary>
/// Подключается к чатам русскоязычных теккен-стримов и пересылает их
/// сообщения в Discord.
/// </summary>
/// <remarks>
/// Перенос <c>TekkenStreamsDiscordForwarderService</c> монолита. Разбит на две
/// части: решения вынесены в <see cref="TekkenStreamsSyncPolicy"/> и
/// проверяются тестами, а этот класс делает только I/O — подключение к чатам,
/// запрос Helix и отправку в Discord.
/// <para>
/// Владелец единственного IRC-подключения — MARS.TwitchCore, поэтому и
/// бо́льшую часть каналов, и пересылку делает он же; MARS.Discord только
/// отправляет сообщение в канал.
/// </para>
/// </remarks>
public class TekkenStreamsDiscordForwarderService(
    ITwitchClient twitchClient,
    ITwitchAPI api,
    IDiscordClient discordClient,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<TekkenStreamsDiscordForwarderService> logger
) : BackgroundService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        byte
    > _tekkenChannels = new(StringComparer.OrdinalIgnoreCase);

    private ulong _discordChannelId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        twitchClient.OnMessageReceived += OnMessageReceived;

        try
        {
            using var timer = new PeriodicTimer(TekkenStreamsSyncPolicy.RefreshInterval);

            await SyncStreamsAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SyncStreamsAsync(stoppingToken);
            }
        }
        finally
        {
            twitchClient.OnMessageReceived -= OnMessageReceived;
        }
    }

    /// <summary>
    /// Обновляет список каналов: выходит из завершившихся стримов и входит в
    /// новые.
    /// </summary>
    public async Task SyncStreamsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _discordChannelId = await GetDiscordChannelIdAsync(cancellationToken);

            var streams = await GetRuTekkenStreamsAsync(cancellationToken);
            var streamLogins = streams
                .Select(stream => stream.UserLogin)
                .Where(login => !string.IsNullOrWhiteSpace(login))
                .ToArray();

            await LeaveStoppedStreamsAsync(streamLogins, cancellationToken);
            await JoinNewStreamsAsync(streamLogins, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Синхронизация теккен-каналов прервана остановкой сервиса");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка синхронизации каналов теккен-стримов");
        }
    }

    /// <summary>
    /// Пересылает сообщение из чата теккен-стрима в Discord. Сообщения своего
    /// канала и чатов, куда пересылка не настроена, отбрасываются.
    /// </summary>
    public async Task OnMessageReceived(object? sender, OnMessageReceivedArgs args)
    {
        var channel = args.ChatMessage.Channel;

        if (
            channel.Equals(TwitchConstants.Channel, StringComparison.OrdinalIgnoreCase)
            || !TekkenStreamsSyncPolicy.ShouldForward(
                channel,
                _tekkenChannels.Keys,
                _discordChannelId
            )
        )
        {
            return;
        }

        var message = TekkenStreamsSyncPolicy.FormatMessage(
            channel,
            args.ChatMessage.DisplayName,
            args.ChatMessage.Message
        );

        var sent = await discordClient.SendMessageAsync(_discordChannelId, message);

        if (sent)
        {
            logger.LogDebug("Переслано сообщение из {Channel} в Discord", channel);
        }
        else
        {
            logger.LogWarning(
                "Не удалось переслать сообщение из канала {Channel} в Discord",
                channel
            );
        }
    }

    private async Task LeaveStoppedStreamsAsync(
        IReadOnlyCollection<string> streamLogins,
        CancellationToken cancellationToken
    )
    {
        var joined = twitchClient.JoinedChannels.Select(channel => channel.Channel).ToArray();
        var toLeave = TekkenStreamsSyncPolicy.BuildToLeave(
            joined,
            streamLogins,
            TwitchConstants.Channel
        );

        foreach (var channel in toLeave)
        {
            await twitchClient.LeaveChannelAsync(channel);
            _tekkenChannels.TryRemove(channel, out _);
            logger.LogInformation("Выход из чата канала {Channel}, стрим завершён", channel);
        }

        await Task.CompletedTask;
    }

    private async Task JoinNewStreamsAsync(
        IReadOnlyCollection<string> streamLogins,
        CancellationToken cancellationToken
    )
    {
        var joined = twitchClient.JoinedChannels.Select(channel => channel.Channel).ToArray();
        var toJoin = TekkenStreamsSyncPolicy.BuildToJoin(
            streamLogins,
            joined,
            TwitchConstants.Channel
        );

        foreach (var login in toJoin)
        {
            await twitchClient.JoinChannelAsync(login);
            _tekkenChannels.TryAdd(login, 0);
            logger.LogInformation("Подключение к чату канала {Channel}", login);

            await Task.Delay(TekkenStreamsSyncPolicy.JoinDelay, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<TwitchStream>> GetRuTekkenStreamsAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<TwitchStream> result = [];

        try
        {
            var response = await api
                .Helix.Streams.GetStreamsAsync(
                    first: 100,
                    gameIds: [TekkenStreamsSyncPolicy.TekkenGameId],
                    languages: [TekkenStreamsSyncPolicy.StreamLanguage]
                )
                .WaitAsync(cancellationToken);

            result = response.Streams;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения списка теккен-стримов");
        }

        return result;
    }

    /// <summary>
    /// ID Discord-канала из <c>RootState</c>. Ноль означает «пересылка не
    /// настроена»: сообщения не отправляются, но сервис работает.
    /// </summary>
    private async Task<ulong> GetDiscordChannelIdAsync(CancellationToken cancellationToken)
    {
        var result = 0UL;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var state = await dbContext
                .RootState.AsNoTracking()
                .SingleOrDefaultAsync(
                    key => key.Name == RootStateKeys.TekkenStreamsDiscordChannelId,
                    cancellationToken
                );

            if (state is not null && ulong.TryParse(state.Value, out var channelId))
            {
                result = channelId;
            }
            else
            {
                logger.LogDebug(
                    "Ключ RootState '{Key}' не задан или имеет некорректное значение",
                    RootStateKeys.TekkenStreamsDiscordChannelId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка чтения ключа RootState '{Key}'",
                RootStateKeys.TekkenStreamsDiscordChannelId
            );
        }

        return result;
    }
}
