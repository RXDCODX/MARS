using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.HelloVideos;

public class HelloVideoWorker(
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<HelloVideoWorker> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IHelloVideoNotifier notifier,
    ITwitchClient client,
    ITwitchEventValidationService validator
) : BackgroundService
{
    private readonly CancellationToken _token = hostApplicationLifetime.ApplicationStopping;

    /// <summary>
    /// Аудит №17: неограниченный List заменён на bounded FIFO-кэш последних
    /// обработанных сообщений, чтобы список не рос бесконечно на долгоживущем канале.
    /// </summary>
    private readonly RecentMessageTracker _recentMessages = new(capacity: 5000);

    public bool IsServiceActive { get; set; } = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.OnMessageReceived += OnMessageReceived;

        // Ждем остановки сервиса
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        client.OnMessageReceived -= OnMessageReceived;
        await base.StopAsync(cancellationToken);
    }

    public async Task OnMessageReceived(object? sender, OnMessageReceivedArgs args)
    {
        var result = await validator
            .ForMessageReceived(args)
            .RequireChannel()
            .RequireServiceActive(IsServiceActive)
            .SkipBlacklisted()
            .ValidateWithResponseAsync(args.ChatMessage.Username);

        if (result.IsInvalid)
        {
            return;
        }

        await Task.Run(
            async () =>
            {
                try
                {
                    // Два разных времени, и это не одно и то же.
                    //
                    // `nowUtc` идёт в сравнение календарных дат с LastTimeNotif,
                    // который приходит из базы с Kind=Utc. При положительном
                    // смещении в первые часы после полуночи локальное сегодня на
                    // день позже прочитанного значения, и «уже показывали сегодня»
                    // переставало быть верным: видео показывалось зрителю повторно.
                    //
                    // `localNow` определяет день недели, потому что «пятница» —
                    // правило для зрителя, а не для сервера. На UTC оно уехало бы
                    // на смещение: при UTC+3 это пятница 03:00, то есть в пятницу
                    // после полуночи видео показывали, а в субботу до трёх — нет.
                    var nowUtc = DateTime.UtcNow;
                    var localNow = DateTime.Now;
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(_token);
                    var user = await dbContext.FumoUsers.FindAsync(
                        [args.ChatMessage.UserId],
                        _token
                    );

                    if (localNow.DayOfWeek == DayOfWeek.Friday && user != null)
                    {
                        return;
                    }

                    if (_recentMessages.TryMarkSeen(args.ChatMessage.Id))
                    {
                        return;
                    }

                    var notifUser = await dbContext
                        .HelloVideosUsers.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.TwitchId == args.ChatMessage.UserId, _token);

                    if (notifUser != null)
                    {
                        if (HelloVideoEligibility.ShouldNotify(notifUser.LastTimeNotif, nowUtc))
                        {
                            notifUser.LastTimeNotif = nowUtc;
                            dbContext.HelloVideosUsers.Update(notifUser);
                            await dbContext.SaveChangesAsync(_token);

                            var alert = new HelloVideoAlertDto
                            {
                                DisplayName = args.ChatMessage.DisplayName,
                                Message = args.ChatMessage.Message,
                                ChatColor = TwitchUser.FromOnMessageReceivedArgs(args)?.ChatColor,
                                MediaInfoId = notifUser.MediaInfoId,
                            };

                            await notifier.SendAlertAsync(alert);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogException(ex);
                }
            },
            _token
        );
    }

    public async Task<string?> TestVideo(string name, string? color = "white")
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_token);
        var user = dbContext
            .HelloVideosUsers.AsNoTracking()
            .Include(e => e.TwitchUser)
            .AsEnumerable()
            .FirstOrDefault(e =>
                e.TwitchUser != null
                && name.Equals(e.TwitchUser.DisplayName, StringComparison.OrdinalIgnoreCase)
            );

        if (user == null)
        {
            return null;
        }

        var alert = new HelloVideoAlertDto
        {
            DisplayName = name,
            Message = string.Empty,
            ChatColor = color,
            MediaInfoId = user.MediaInfoId,
        };

        await notifier.SendAlertAsync(alert);
        return user.TwitchUser?.DisplayName;
    }
}
