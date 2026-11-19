using System.Collections.Concurrent;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace MARS.Gateway.Swagger;

public class SwaggerAggregatorService(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceEndpoints> serviceEndpoints,
    ILogger<SwaggerAggregatorService> logger
) : BackgroundService
{
    /// <summary>
    /// Аудит: карта endpoint'ов была захардкожена и расходилась с секцией
    /// ServiceEndpoints (не содержала Alerts/Discord/TTS/Scoreboard). Теперь она
    /// строится из конфигурации — см. <see cref="SwaggerEndpointMap"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> ServiceEndpoints =>
        SwaggerEndpointMap.Build(serviceEndpoints.Value);

    /// <summary>
    /// Документы SignalR-хабов: отдельный словарь, потому что это отдельный
    /// документ Swashbuckle (<c>hubs</c>), а не часть REST-спецификации.
    /// </summary>
    public IReadOnlyDictionary<string, string> HubsEndpoints =>
        SwaggerEndpointMap.BuildHubs(serviceEndpoints.Value);

    private readonly ConcurrentDictionary<string, OpenApiDocument> _cachedDocs = new();
    private readonly ConcurrentDictionary<string, OpenApiDocument> _cachedHubsDocs = new();

    public IReadOnlyDictionary<string, OpenApiDocument> CachedDocs => _cachedDocs;

    public IReadOnlyDictionary<string, OpenApiDocument> CachedHubsDocs => _cachedHubsDocs;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAllSpecs(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task FetchAllSpecs(CancellationToken cancellationToken)
    {
        var endpoints = ServiceEndpoints;

        logger.LogInformation(
            "Swagger aggregation: discovering {Count} services ({Services})",
            endpoints.Count,
            string.Join(", ", endpoints.Keys)
        );

        await FetchSpecs(endpoints, _cachedDocs, isHubs: false, cancellationToken);
        await FetchSpecs(HubsEndpoints, _cachedHubsDocs, isHubs: true, cancellationToken);
    }

    private async Task FetchSpecs(
        IReadOnlyDictionary<string, string> endpoints,
        ConcurrentDictionary<string, OpenApiDocument> cache,
        bool isHubs,
        CancellationToken cancellationToken
    )
    {
        var client = httpClientFactory.CreateClient();

        foreach (var (name, url) in endpoints)
        {
            try
            {
                var response = await client.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // Хаб-документ есть не у каждого сервиса, поэтому 404 — это
                    // норма, а не повод для предупреждения в логе.
                    if (isHubs && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        continue;
                    }

                    logger.LogWarning(
                        "Swagger spec request to {Service} returned {StatusCode} ({Url})",
                        name,
                        (int)response.StatusCode,
                        url
                    );
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(
                    cancellationToken
                );
                var result = await OpenApiDocument.LoadAsync(
                    stream,
                    cancellationToken: cancellationToken
                );

                if (result.Document is null)
                {
                    logger.LogWarning(
                        "Swagger spec from {Service} could not be parsed: {Errors}",
                        name,
                        result.Diagnostic?.Errors is { Count: > 0 } errors
                            ? string.Join("; ", errors.Select(error => error.Message))
                            : "unknown error"
                    );
                    continue;
                }

                cache[name] = result.Document;
                logger.LogInformation(
                    "Swagger {Kind} spec fetched from {Service}",
                    isHubs ? "hubs" : "REST",
                    name
                );
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch swagger from {Service}", name);
            }
        }
    }
}
