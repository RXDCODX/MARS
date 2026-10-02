using System.Net;
using System.Text;
using MARS.Gateway.Swagger;
using MARS.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Gateway.Tests;

/// <summary>
/// Агрегатор Swagger шлюза: собирает спецификации сервисов и раздаёт их по
/// <c>/swagger/{service}/swagger.json</c>.
///
/// Спецификации отдаются настоящим HTTP-клиентом с подставленным обработчиком:
/// проверять здесь нечего, кроме того, что успешный ответ разбирается и попадает
/// в кэш, а неуспешный — нет.
/// </summary>
public class SwaggerAggregatorServiceTests
{
    private const string Spec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "MARS.OBS", "version": "1.0" },
          "paths": {}
        }
        """;

    [Fact]
    public async Task SpecIsFetchedPerConfiguredServiceAndCached()
    {
        var endpoints = new ServiceEndpoints
        {
            OBS = "http://obs:8080/swagger/v1/swagger.json",
            TTS = "http://tts:8080/swagger/v1/swagger.json",
        };
        var aggregator = new SwaggerAggregatorService(
            ClientFactory(HttpStatusCode.OK, Spec),
            Options.Create(endpoints),
            NullLogger<SwaggerAggregatorService>.Instance
        );

        await StartAndStopAsync(aggregator);

        // ServiceEndpoints заполнен значениями по умолчанию, поэтому проверяются
        // заданные сервисы, а не весь набор: иначе тест ломался бы от любого
        // нового сервиса в конфигурации.
        Assert.Contains("OBS", aggregator.CachedDocs.Keys);
        Assert.Contains("TTS", aggregator.CachedDocs.Keys);
        Assert.Equal("MARS.OBS", aggregator.CachedDocs["OBS"].Info.Title);
    }

    /// <summary>
    /// Неуспешный ответ не должен ронять весь обход: иначе один упавший сервис
    /// убирал бы из UI все остальные.
    /// </summary>
    [Fact]
    public async Task FailedServiceDoesNotStopAggregation()
    {
        var endpoints = new ServiceEndpoints
        {
            OBS = "http://obs:8080/swagger/v1/swagger.json",
            TTS = "http://tts:8080/swagger/v1/swagger.json",
        };
        var aggregator = new SwaggerAggregatorService(
            ClientFactory(HttpStatusCode.ServiceUnavailable, string.Empty),
            Options.Create(endpoints),
            NullLogger<SwaggerAggregatorService>.Instance
        );

        await StartAndStopAsync(aggregator);

        Assert.Empty(aggregator.CachedDocs);
    }

    [Fact]
    public async Task UnparseableSpecIsNotCached()
    {
        var endpoints = new ServiceEndpoints { OBS = "http://obs:8080/swagger/v1/swagger.json" };
        var aggregator = new SwaggerAggregatorService(
            ClientFactory(HttpStatusCode.OK, "не json вовсе"),
            Options.Create(endpoints),
            NullLogger<SwaggerAggregatorService>.Instance
        );

        await StartAndStopAsync(aggregator);

        Assert.Empty(aggregator.CachedDocs);
    }

    [Fact]
    public void EndpointsComeFromConfiguration()
    {
        var aggregator = new SwaggerAggregatorService(
            ClientFactory(HttpStatusCode.OK, Spec),
            Options.Create(new ServiceEndpoints { OBS = "http://obs:8080/", TTS = string.Empty }),
            NullLogger<SwaggerAggregatorService>.Instance
        );

        var endpoints = aggregator.ServiceEndpoints;

        Assert.Contains("OBS", endpoints.Keys);
        Assert.DoesNotContain("TTS", endpoints.Keys);
    }

    /// <summary>
    /// Регистрация обязана отдавать один и тот же экземпляр сервиса и как
    /// singleton, и как фоновая задача: иначе UI читал бы кэш одного объекта, а
    /// наполнял бы другой.
    /// </summary>
    [Fact]
    public void RegistrationKeepsSingleInstanceForServiceAndHostedService()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Options.Create(new ServiceEndpoints()))
            .AddHttpClient()
            .AddMarsSwaggerAggregator()
            .BuildServiceProvider();

        var hosted = provider.GetServices<IHostedService>();

        Assert.Single(hosted);
        Assert.Same(provider.GetRequiredService<SwaggerAggregatorService>(), hosted.Single());
    }

    private static IHttpClientFactory ClientFactory(HttpStatusCode status, string body)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(new StubHandler(status, body)) { BaseAddress = null };
        factory.Setup(instance => instance.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static async Task StartAndStopAsync(SwaggerAggregatorService aggregator)
    {
        await aggregator.StartAsync(TestContext.Current.CancellationToken);

        // ExecuteAsync зациклен с паузой в пять минут, поэтому ждать его нельзя:
        // достаточно дождаться первого обхода и остановить сервис.
        for (var attempt = 0; attempt < 50 && aggregator.CachedDocs.Count == 0; attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        await aggregator.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
