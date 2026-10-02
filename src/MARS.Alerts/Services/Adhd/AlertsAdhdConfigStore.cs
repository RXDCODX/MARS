using System.Text.Json;
using MARS.Alerts.Models;
using MARS.Alerts.Services.Adhd;
using MARS.Shared.Grpc;

namespace MARS.Alerts.Services.Adhd;

/// <summary>
/// Мост между контрактом <c>TelegramusService</c> и таблицей настройки в
/// AlertsDb. Настройка едет JSON-байтами — так же, как остальные объектные поля
/// контракта.
/// </summary>
public sealed class AlertsAdhdConfigStore(IAdhdLayoutService layoutService) : IAdhdConfigStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        return await ReadAsync(cancellationToken);
    }

    public async Task<string> UpdateAsync(string configJson, CancellationToken cancellationToken)
    {
        AdhdLayoutConfigDto? dto = null;

        if (!string.IsNullOrWhiteSpace(configJson))
        {
            try
            {
                dto = JsonSerializer.Deserialize<AdhdLayoutConfigDto>(configJson, Options);
            }
            catch (JsonException)
            {
                dto = null;
            }
        }

        if (dto is not null)
        {
            var written = await layoutService.UpdateAsync(dto, cancellationToken);
            return written.Result is not null
                ? JsonSerializer.Serialize(written.Result, Options)
                : configJson;
        }

        return await ReadAsync(cancellationToken);
    }

    private async Task<string> ReadAsync(CancellationToken cancellationToken)
    {
        var read = await layoutService.GetAsync(cancellationToken);

        return read.Result is not null
            ? JsonSerializer.Serialize(read.Result, Options)
            : JsonSerializer.Serialize(new AdhdLayoutConfigDto(), Options);
    }
}
