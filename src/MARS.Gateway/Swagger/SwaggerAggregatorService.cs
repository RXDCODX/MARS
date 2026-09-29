using System.Collections.Concurrent;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

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

    private readonly ConcurrentDictionary<string, OpenApiDocument> _cachedDocs = new();

    public IReadOnlyDictionary<string, OpenApiDocument> CachedDocs => _cachedDocs;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await FetchAllSpecs(ct);
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    private async Task FetchAllSpecs(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var endpoints = ServiceEndpoints;

        logger.LogInformation(
            "Swagger aggregation: discovering {Count} services ({Services})",
            endpoints.Count,
            string.Join(", ", endpoints.Keys)
        );

        foreach (var (name, url) in endpoints)
        {
            try
            {
                var response = await client.GetAsync(url, ct);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(ct);
                    var doc = new OpenApiStreamReader().Read(stream, out _);

                    if (doc == null)
                    {
                        logger.LogWarning("Swagger spec from {Service} could not be parsed", name);
                    }
                    else
                    {
                        _cachedDocs[name] = doc;
                        logger.LogInformation("Swagger spec fetched from {Service}", name);
                    }
                }
                else
                {
                    logger.LogWarning(
                        "Swagger spec request to {Service} returned {StatusCode} ({Url})",
                        name,
                        (int)response.StatusCode,
                        url
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch swagger from {Service}", name);
            }
        }
    }
}
