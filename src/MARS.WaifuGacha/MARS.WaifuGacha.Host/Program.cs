using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Kernel.Concurrency;
using MARS.WaifuGacha.Infrastructure.Postgres.Configuration;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Host;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.WaifuGacha", "WaifuDb");
        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsGrpcClient();
        // Второй аргумент — сборка Infrastructure.Postgres: листенеры
        // RollCooldownChatListener и RollCooldownRewardListener живут теперь
        // в ней, а Wolverine ищет MarsEventListener<T> только по переданным
        // сборкам. Без неё кулдаун-уведомления молча перестают приходить.
        builder.AddMarsWolverine(
            "MARS.WaifuGacha",
            typeof(Program).Assembly,
            typeof(WaifuDbContext).Assembly
        );

        // Database
        builder.Services.AddMarsDbContext<WaifuDbContext>(
            builder.Configuration,
            "waifu",
            "WaifuDb"
        );

        // Адрес сайта Shikimori нужен только для нормализации ссылок от UI.
        // Сам клиент и рейт-лимитер живут в MARS.Shikimori.
        builder.Services.Configure<ShikimoriSiteOptions>(
            builder.Configuration.GetSection(ShikimoriSiteOptions.SectionName)
        );

        // Единственный владелец общего per-key лока сервиса (роллы + auto-hello)
        builder.Services.AddSingleton<KeyedAsyncLock>();

        // Кудауны роллов читаются из waifu.RootState, а не зашиты в коде
        builder.Services.AddSingleton<RollCooldownConfigurationService>();
        builder.Services.AddSingleton<RollCooldownService>();
        builder.Services.AddSingleton<RollCooldownNotificationState>();
        builder.Services.AddHostedService<RootStateBootstrapHostedService>();

        // Межсервисные вызовы идут через gRPC (IMarsGrpcClient); REST-клиенты
        // Shikimori/Twitch больше не используются в WaifuGacha.

        // Services
        builder.Services.AddSingleton<WaifuRollEnsurenceService>();
        builder.Services.AddSingleton<WaifuRollService>();
        builder.Services.AddScoped<WaifuPrizesService>();
        builder.Services.AddScoped<WaifuRollGuaranteeService>();
        builder.Services.AddScoped<MergeWaifuService>();
        builder.Services.AddScoped<AddNewWaifuService>();
        builder.Services.AddScoped<FumoRollService>();
        builder.Services.AddScoped<FrogRollService>();
        builder.Services.AddScoped<MikuRollService>();
        builder.Services.AddScoped<FumoCollectionService>();
        builder.Services.AddScoped<MikuCollectionService>();
        builder.Services.AddScoped<MikuMondayService>();

        // Уведомления о завершении кулдауна ролла теперь обрабатываются
        // Wolverine-лисенерами RollCooldownRewardListener и
        // RollCooldownChatListener, регистрируемыми автоматически.
        builder.Services.AddSingleton<WeddingAnniversaryService>();
        builder.Services.AddSingleton<AutoHelloService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        app.MapGrpcService<MARS.WaifuGacha.Host.Grpc.WaifuGachaGrpcService>()
            .RequireAuthorization(ServiceAuthExtensions.PolicyName);

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        await app.RunMarsSchemaMigrationsAsync();

        await app.RunAsync();
    }
}
