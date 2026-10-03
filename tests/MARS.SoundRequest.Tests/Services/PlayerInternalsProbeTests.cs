using System.Reflection;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.SoundBarService;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Внутренние решения плеера и звукового сервиса.
///
/// Очередь выбирается по номеру один, а не по дате: трек, поставленный первым,
/// должен играть первым даже после перезапуска. Проверка режима запуска нужна,
/// потому что в контейнере звуковой сервис поднимается системной службой.
/// </summary>
public class PlayerInternalsProbeTests
{
    /// <summary>
    /// Следующий трек — тот, у которого порядок равен единице, независимо от даты
    /// добавления: иначе после перезапуска очередь игралась бы не с начала.
    /// </summary>
    [Fact]
    public async Task NextQueueItemIsTakenByQueueOrder()
    {
        var factory = new TestDbContextFactory();
        var track = new BaseTrackInfo
        {
            TrackName = "трек",
            Url = new Uri("https://mars.example.org/трек"),
            Duration = TimeSpan.FromMinutes(1),
        };

        await using (var context = factory.CreateDbContext())
        {
            context.Tracks.Add(track);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.QueueItems.Add(
                new QueueItem
                {
                    TrackId = track.Id,
                    Track = track,
                    QueueOrder = 1,
                    RequestedByTwitchId = "зритель",
                }
            );
            context.QueueItems.Add(
                new QueueItem
                {
                    TrackId = track.Id,
                    Track = track,
                    QueueOrder = 2,
                    RequestedByTwitchId = "зритель",
                }
            );
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var stateManager = new StateManager(
            factory,
            new TestHostApplicationLifetime(),
            NullLogger<StateManager>.Instance
        );
        var next = await InvokeAsync<QueueItem?>(stateManager, "GetNextQueueItemAsync", []);

        Assert.NotNull(next);
        Assert.Equal(1, next.QueueOrder);
    }

    /// <summary>
    /// Пустая очередь возвращает null, а не исключение: следующего трека просто нет,
    /// и плеер должен остановиться, а не упасть.
    /// </summary>
    [Fact]
    public async Task EmptyQueueHasNoNextItem()
    {
        var factory = new TestDbContextFactory();
        var stateManager = new StateManager(
            factory,
            new TestHostApplicationLifetime(),
            NullLogger<StateManager>.Instance
        );

        var next = await InvokeAsync<QueueItem?>(stateManager, "GetNextQueueItemAsync", []);

        Assert.Null(next);
    }

    /// <summary>
    /// Режим запуска совпадает с системой: в контейнере (Linux) звуковой сервис не
    /// считается службой Windows иначе и поднимал бы себя в интерактивном режиме.
    /// </summary>
    [Fact]
    public void WindowsServiceModeFollowsPlatform()
    {
        var runningAsService = InvokeStatic<bool>("IsRunningAsWindowsService");

        Assert.Equal(OperatingSystem.IsWindows() && IsWindowsService(), runningAsService);
    }

    private static bool IsWindowsService() =>
        Environment.UserInteractive == false
        && Environment.OSVersion.Platform == PlatformID.Win32NT;

    private static T InvokeStatic<T>(string name)
    {
        var method = typeof(SoundBarFactory).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (T)(method.Invoke(null, null) ?? default(T))!;
    }

    private static Task<T> InvokeAsync<T>(object target, string name, object?[] arguments)
    {
        var method = target
            .GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (Task<T>)method.Invoke(target, arguments)!;
    }
}
