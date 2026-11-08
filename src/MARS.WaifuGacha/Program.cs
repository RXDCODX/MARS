using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Shared.Kernel.Concurrency;
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
        builder.AddMarsWolverine("MARS.WaifuGacha", typeof(Program).Assembly);

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
        builder.Services.AddHostedService<RootStateBootstrapHostedService>();

        // Shikimori: собственного клиента больше нет, только HTTP-клиент сервиса.
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IShikimoriApiClient, ShikimoriApiClient>(
            ServiceClientExtensions.ShikimoriHttpClientName
        );

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

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
