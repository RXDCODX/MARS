using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Alerts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/AlertsCommandClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/AlertsCommandClient.cs

public class AlertsCommandClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<AlertsCommandClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IAlertsCommandClient
{
    private const string InvokeUrl = "api/internal/commands/invoke";

    public override string ServiceEndpoint => endpoints.Value.Alerts;

    public async Task<string> InvokeAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        var result = await PostAsync<AlertCommandPayload, string>(
            InvokeUrl,
            payload,
            cancellationToken
        );
        return result ?? "Команда не выполнена: сервис недоступен.";
    }
}
