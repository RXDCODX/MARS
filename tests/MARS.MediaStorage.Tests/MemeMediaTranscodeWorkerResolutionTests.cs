using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Telegram;
using MARS.Shared.Configuration;
using MARS.Shared.Models.Media;
using MARS.Shared.Telegram;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Воркер перекодирования должен собираться контейнером и без токена Telegram.
/// </summary>
/// <remarks>
/// Клиент Telegram регистрируется в <c>Program.cs</c> только когда токен задан.
/// Раньше вместе с ним не регистрировался и <see cref="ITelegramAdminMessenger"/>,
/// а параметр воркера был объявлен nullable — но nullable-аннотация для контейнера
/// DI не значит ничего: <c>AddHostedService&lt;T&gt;</c> требует, чтобы зависимость
/// была зарегистрирована, и иначе хост падает на старте.
/// <para>
/// На стенде это выглядело как «медиа-хранилище не запускается»: контейнер был
/// unhealthy, а навигационные тесты клиента не могли подняться, хотя токен
/// Telegram к перекодированию мемов отношения не имеет.
/// </para>
/// <para>
/// Тест собирает контейнер так же, как это делает <c>Program.cs</c>, и создаёт
/// воркер через <c>ActivatorUtilities</c> — тем же путём, каким его создаёт
/// контейнер. Прямой вызов конструктора проверял бы только то, что он принимает
/// <c>null</c>, и не поймал бы возврат DI на старте хоста.
/// </para>
/// </remarks>
public class MemeMediaTranscodeWorkerResolutionTests
{
    [Fact]
    public void WorkerResolvesWithoutTelegramToken()
    {
        var memes = new Mock<IRandomMemeService>();
        var services = new ServiceCollection();

        services.AddSingleton(memes.Object);
        services.AddSingleton(
            new MemeMediaPreparationService(
                Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == Directory.GetCurrentDirectory()),
                Mock.Of<IMediaInspector>(),
                Mock.Of<IMediaTranscoder>(),
                NullLogger<MemeMediaPreparationService>.Instance
            )
        );
        services.AddSingleton(Options.Create(new TelegramConfig()));
        services.AddSingleton<ILogger<MemeMediaTranscodeWorker>>(
            NullLogger<MemeMediaTranscodeWorker>.Instance
        );

        // Ветка без токена: настоящий мессенджер не подходит, потому что ему нужен
        // ITelegramBotClient, а его тоже нет.
        services.AddSingleton<ITelegramAdminMessenger, NullTelegramAdminMessenger>();

        using var provider = services.BuildServiceProvider();

        var worker = ActivatorUtilities.CreateInstance<MemeMediaTranscodeWorker>(provider);

        Assert.NotNull(worker);
    }

    /// <summary>
    /// Без токена отчёт не уходит, но перекодирование не падает.
    /// </summary>
    /// <remarks>
    /// Отчёт — удобство, а не условие работы. Заглушка обязана завершаться
    /// задачей, иначе отсутствие Telegram снова становится поводом остановить
    /// фоновый проход.
    /// </remarks>
    [Fact]
    public async Task NullMessengerDoesNotThrow()
    {
        var messenger = new NullTelegramAdminMessenger();

        await messenger.SendAsync(
            chatId: 1,
            text: "перекодировано",
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
