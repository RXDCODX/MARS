using MARS.Shared.Authentication;
using MARS.Shared.Development;
using MARS.Shared.Middleware;
using MARS.Shared.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace MARS.Shared.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Стандартный pipeline для всех MARS сервисов.
        /// </summary>
        /// <param name="includeSwagger">
        /// false отключает собственный Swagger-UI сервиса. Нужен Gateway: там
        /// используется агрегатор (<c>UseMarsSwaggerAggregator</c>), а два
        /// UseSwaggerUI на префиксе /swagger конфликтуют — побеждает первый
        /// зарегистрированный, и список сервисов не отображается.
        /// </param>
        public WebApplication UseMarsDefaults(bool includeSwagger = true)
        {
            // Поднять недостающую инфраструктуру стенда — до применения миграций, и
            // это требование, а не удобство: `UseMarsDefaults` в каждом Program.cs
            // стоит перед `RunMarsSchemaMigrationsAsync`, и без этого шага F5 на
            // выключенном стенде ждёт базу около минуты и падает. Шаг сам решает по
            // флагу `Dev:AutoStartInfra` и окружению, поэтому в контейнере он ничего
            // не делает; отсутствие регистрации (тонкий тестовый хост) — тоже не
            // ошибка.
            app.Services.GetService<IDevInfrastructureStarter>()?.EnsureStarted();

            // Цель Prometheus для процесса, запущенного на хосте: файл кладёт сам
            // сервис, его читает задача mars-local в dev-стенде.
            app.Services.GetService<IDevPrometheusTargetRegistrar>()?.Register();

            // Объявить цель Prometheus для процесса на хосте. Тот же характер,
            // что и у автостарта: шаг сам решает по окружению и Dev:MetricsPort,
            // поэтому в контейнере он молчит, а в тонком тестовом хосте
            // отсутствие регистрации не ошибка.
            app.Services.GetService<IDevPrometheusTargetRegistrar>()?.Register();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseMiddleware<CorrelationIdMiddleware>();

            if (includeSwagger)
            {
                app.UseSwaggerInAllEnvironments();
            }

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpMetrics();
            app.UseMetricServer();

            _ = app.Services.GetRequiredService<OpenTelemetryPrometheusBridge>();

            app.MapHealthChecks("/health");
            app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }
            );
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

            return app;
        }

        /// <summary>
        /// Swagger доступен во всех окружениях: агрегатор в Gateway и compose-стек
        /// полагаются на него в Production.
        /// </summary>
        private void UseSwaggerInAllEnvironments()
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    /// <summary>
    /// Регистрирует маршруты и кластеры, доступные только с корректным API-ключом.
    /// </summary>
    public static void RequireServiceApiKey(this RouteHandlerBuilder builder)
    {
        builder.RequireAuthorization(ServiceAuthExtensions.PolicyName);
    }
}
