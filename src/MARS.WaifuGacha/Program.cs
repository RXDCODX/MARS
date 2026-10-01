using MARS.Shared.Concurrency;
using MARS.Shared.Extensions;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Services;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.WaifuGacha", "WaifuDb");

        // Database
        builder.Services.AddMarsDbContext<WaifuDbContext>(builder.Configuration, "waifu", "WaifuDb");

        // Options
        builder.Services.Configure<ShikimoriClientOptions>(
            builder.Configuration.GetSection("Shikimori")
        );

        // Rate limiter
        builder.Services.AddSingleton<IShikimoriRateLimiter, ShikimoriRateLimiter>();

        // Единственный владелец общего per-key лока сервиса (роллы + auto-hello)
        builder.Services.AddSingleton<KeyedAsyncLock>();

        // Кудауны роллов читаются из waifu.RootState, а не зашиты в коде
        builder.Services.AddSingleton<RollCooldownConfigurationService>();
        builder.Services.AddSingleton<RollCooldownService>();
        builder.Services.AddHostedService<RootStateBootstrapHostedService>();

        // Services
        builder.Services.AddSingleton<ShikimoriService>();
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

        // Уведомления «кулдаун прошёл» идут через шину: собственного IRC-подключения
        // у MARS.WaifuGacha больше нет, иначе Twitch закрывает одно из двух соединений
        // бот-аккаунта (блокер №19).
        builder.Services.AddHostedService<RollCooldownNotificationService>();

        // Auto-hello (область владения супругами) + годовщины свадьбы
        builder.Services.AddSingleton<WeddingAnniversaryService>();
        builder.Services.AddSingleton<AutoHelloService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
