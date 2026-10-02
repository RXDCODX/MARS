using System.Net;
using MARS.Shared.Extensions;
using MARS.Shared.Telemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Tests;

/// <summary>
/// Общий pipeline сервисов.
///
/// Каждый микросервис поднимает одинаковые эндпоинты: `/health` для балансировщика,
/// `/health/ready`, `/health/live` и `/metrics` для Prometheus. Проверяется, что они
/// действительно отвечают: иначе стенд считался бы здоровым, а панели Grafana были
/// бы пустыми.
///
/// Swagger в этих проверках выключен: сам документ собирается из XML-описаний всех
/// сервисов и без них падает при первом же запросе, то есть проверял бы не pipeline.
/// </summary>
public class WebApplicationExtensionsTests : IAsyncLifetime
{
    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        _app = Build();
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthEndpointsAnswer(string path)
    {
        using var client = Client();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Метрики отдаются текстом prometheus: иначе Grafana не собрала бы панели.
    /// </summary>
    [Fact]
    public async Task MetricsEndpointAnswers()
    {
        using var client = Client();

        var response = await client.GetAsync("/metrics", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("# HELP", body);
    }

    /// <summary>
    /// Мост метрик поднимается вместе с приложением: без него счётчики сервисов не
    /// попадали бы в /metrics.
    /// </summary>
    [Fact]
    public void MetricsBridgeIsResolved()
    {
        Assert.NotNull(_app.Services.GetService<OpenTelemetryPrometheusBridge>());
    }

    /// <summary>
    /// Включение Swagger не ломает сборку pipeline: у Gateway он выключается
    /// (там агрегатор), у сервисов включается.
    /// </summary>
    [Fact]
    public async Task SwaggerCanBeEnabled()
    {
        var app = Build(includeSwagger: true);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    private WebApplication Build(bool includeSwagger = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        builder.Environment.EnvironmentName = Microsoft.Extensions.Hosting.Environments.Development;
        builder.Services.AddRouting();
        builder.Services.AddCors();
        builder.Services.Configure<CorsOptions>(
            "CorsPolicy",
            options => options.AddPolicy("CorsPolicy", policy => policy.AllowAnyHeader())
        );
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddAuthorization();
        builder
            .Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, AllowAllAuthenticationHandler>(
                "Test",
                _ => { }
            );
        builder.Services.AddSingleton<OpenTelemetryPrometheusBridge>();

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseMarsDefaults(includeSwagger);

        return app;
    }

    private HttpClient Client() => new() { BaseAddress = new Uri(_app.Urls.First()) };

    /// <summary>
    /// Схема аутентификации, пропускающая всё: проверяется pipeline, а не политики.
    /// </summary>
    private sealed class AllowAllAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
