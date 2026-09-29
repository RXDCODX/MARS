using System.Net;
using System.Net.Http;
using MARS.Alerts.Models;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services;

public class Worker365(
    IOptions<Config365> options,
    IHttpClientFactory httpClientFactory,
    IHostApplicationLifetime lifetime,
    IHostEnvironment environment,
    ILogger<Worker365> logger
) : IHostedService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            try
            {
                await Task.Factory.StartNew(Main, _cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in Worker365");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Main()
    {
        if (!ValidateConfig(options.Value))
        {
            logger.LogWarning("Worker365 skipped: Config365 is not fully configured");
            return;
        }

        var site = new Uri(options.Value.Site);
        var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.GetAsync(site, _cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException();
        }

        var sessionId = GetPhpSessionId(site, response);

        var cookies = CreateLogPassCookies(site);
        cookies.Add(site, sessionId);
        httpClient.DefaultRequestHeaders.Add("cookie", cookies.GetCookieHeader(site));

        logger.LogInformation("Worker365 started for site: {Site}", site);
    }

    private static bool ValidateConfig(Config365? optionsValue)
    {
        if (optionsValue is null)
        {
            return false;
        }

        var result =
            !string.IsNullOrWhiteSpace(optionsValue.Login)
            && !string.IsNullOrWhiteSpace(optionsValue.Password)
            && !string.IsNullOrWhiteSpace(optionsValue.Site)
            && optionsValue.TelegramChannelId != 0;

        return result;
    }

    private CookieContainer CreateLogPassCookies(Uri site)
    {
        var container = new CookieContainer();
        container.Add(site, new Cookie("login", options.Value.Login));
        container.Add(site, new Cookie("password", options.Value.Password));
        return container;
    }

    private Cookie GetPhpSessionId(Uri site, HttpResponseMessage response)
    {
        var container = new CookieContainer();
        var responseCookies = response.Headers.GetValues("Set-Cookie");
        foreach (var responseCookie in responseCookies)
        {
            container.SetCookies(site, responseCookie);
        }

        var cookie = new Cookie(
            "PHPSESSID",
            container.GetCookies(site).FirstOrDefault(c => c.Name == "PHPSESSID")?.Value
        );

        return cookie;
    }
}
