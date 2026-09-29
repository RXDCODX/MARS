using System.Reflection;
using MARS.Shared.Configuration;

namespace MARS.Gateway.Swagger;

/// <summary>
/// Строит карту swagger-endpoint'ов из секции <c>ServiceEndpoints</c>.
/// Аудит: раньше карта была захардкожена в <c>SwaggerAggregatorService</c> и
/// расходилась с конфигурацией (не содержала Alerts, Discord, TTS, Scoreboard),
/// а обратное отображение делалось по <c>typeof(ServiceEndpoints).GetProperties()</c> —
/// опечатка в имени свойства тихо ломала бы маршрут.
/// </summary>
public static class SwaggerEndpointMap
{
    public const string SpecPath = "/swagger/v1/swagger.json";

    public static Dictionary<string, string> Build(ServiceEndpoints endpoints)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(ServiceEndpoints).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var value = property.GetValue(endpoints) as string;

            if (!string.IsNullOrWhiteSpace(value))
            {
                result[property.Name] = value.TrimEnd('/') + SpecPath;
            }
        }

        return result;
    }
}
