using MARS.Shared.Exceptions;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Tests.Extensions;

/// <summary>
/// Вспомогательные регистрации общего кода.
///
/// Это то, что вызывает каждый сервис при старте: если регистрация молча
/// перестанет работать, сервис поднимется без health-check, без логирования или
/// без синхронных миграций — и упадёт уже на первом запросе к таблице.
/// </summary>
public class SharedStartupExtensionsTests
{
    /// <summary>
    /// Внутренние эндпоинты отвечают и называют сервис: по ним видно, какой
    /// контейнер жив, а не «где-то в compose».
    /// </summary>
    [Fact]
    public async Task InternalEndpointsAreMapped()
    {
        await using var app = Build();
        app.MapMarsInternalEndpoints("MARS.Probe");

        var routes = Routes(app);

        Assert.Contains(routes, endpoint => endpoint.RoutePattern.RawText == "/internal/health");
        Assert.Contains(routes, endpoint => endpoint.RoutePattern.RawText == "/internal/info");
    }

    /// <summary>
    /// Имя сервиса попадает в ответ health-check: без него в сводке стенда не
    /// понять, кто отвечает.
    /// </summary>
    [Fact]
    public async Task InternalHealthReportsServiceName()
    {
        await using var app = Build();
        app.MapMarsInternalEndpoints("MARS.Probe");

        var health = Routes(app)
            .Single(endpoint => endpoint.RoutePattern.RawText == "/internal/health");
        var context = new DefaultHttpContext();
        // Результаты пишутся через службы приложения (опции сериализации JSON),
        // поэтому контексту нужны те же службы, что и у живого запроса.
        context.RequestServices = app.Services;
        context.Response.Body = new MemoryStream();

        await health.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Contains("MARS.Probe", body);
    }

    /// <summary>
    /// Синхронный прогон миграций вызывается для каждой зарегистрированной схемы:
    /// без него фоновые сервисы стартуют раньше, чем появятся их таблицы.
    /// </summary>
    [Fact]
    public async Task SchemaMarkersAreMigratedBeforeStart()
    {
        var migrated = new List<string>();
        ILogger? given = null;
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(
            new MarsSchemaStartupMarker(
                "twitch",
                logger => given = logger,
                _ =>
                {
                    migrated.Add("twitch");
                    return Task.CompletedTask;
                }
            )
        );
        await using var app = builder.Build();

        await app.RunMarsSchemaMigrationsAsync();

        Assert.Equal(["twitch"], migrated);
        // Логгер передаётся до прогона: иначе падение миграции осталось бы без
        // записи в журнал, и сервис поднялся бы с неполной схемой.
        Assert.NotNull(given);
    }

    /// <summary>
    /// Без зарегистрированных схем прогон ничего не делает: сервисы без базы не
    /// должны падать на этом шаге.
    /// </summary>
    [Fact]
    public async Task SchemaRunWithoutMarkersIsNoOp()
    {
        var builder = WebApplication.CreateBuilder();
        await using var app = builder.Build();

        await app.RunMarsSchemaMigrationsAsync();
    }

    /// <summary>
    /// Стандартная настройка сервиса поднимает фабрику логгеров и health check:
    /// без них сервис не попал бы ни в один из них.
    /// </summary>
    [Fact]
    public async Task DefaultsRegisterCommonServices()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = string.Empty;

        builder.AddMarsDefaults("MARS.Probe", dbConnectionName: null);

        await using var app = builder.Build();

        Assert.NotNull(app.Services.GetService<ILoggerFactory>());
    }

    /// <summary>
    /// Ошибка сервиса хранит и текст, и исходное исключение: без исходника в
    /// журнале не будет причины сбоя.
    /// </summary>
    [Fact]
    public void MarsExceptionKeepsInnerException()
    {
        var inner = new InvalidOperationException("причина");
        var exception = new MarsException("ошибка сервиса", inner);

        Assert.Equal("ошибка сервиса", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    /// <summary>
    /// Имя недоступного сервиса попадает в исключение: по нему видно, к кому
    /// идти чинить зависимость.
    /// </summary>
    [Fact]
    public void ServiceUnavailableNamesTheService()
    {
        var exception = new ServiceUnavailableException("MARS.TwitchCore");

        Assert.Equal("MARS.TwitchCore", exception.ServiceName);
        Assert.Contains("MARS.TwitchCore", exception.Message);
    }

    private static WebApplication Build() => WebApplication.CreateBuilder().Build();

    private static IEnumerable<RouteEndpoint> Routes(IEndpointRouteBuilder app) =>
        app.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
}
