using System.Net;
using MARS.Videos365.Configuration;
using Microsoft.Extensions.Options;

namespace MARS.Videos365.Services;

/// <summary>
/// Конвейер публикации видео в Telegram-канал.
/// </summary>
/// <remarks>
/// Текущее состояние конвейера — заглушка: она проверяет доступность источника
/// (DNS + HTTP, пункт B4) и завершает работу. Обход списка, загрузка и отправка в
/// Telegram не перенесены из монолита, поэтому таблица Videos365 пока только
/// накапливается миграцией.
/// TODO(365): восстановить обход источника и публикацию. Дедупликацию вести по
/// SiteId через Videos365DbContext — уникальный индекс на SiteId уже создан.
/// </remarks>
public class Worker365(
    IOptions<Config365> options,
    IHttpClientFactory httpClientFactory,
    IHostApplicationLifetime lifetime,
    IHostEnvironment environment,
    SiteAvailabilityChecker siteAvailabilityChecker,
    SiteUnavailableNotifier siteUnavailableNotifier,
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
        if (!options.Value.IsComplete())
        {
            logger.LogWarning("Worker365 skipped: Config365 is not fully configured");
            return;
        }

        var site = new Uri(options.Value.Site);

        // Проверка доступности отделена от обхода: её провал означает, что
        // источник лежит, а не что избранка пуста. Администраторам уходит
        // уведомление, проход завершается, и следующий запуск делает то же
        // само — без пометки видео «загружено», которого в канале нет.
        var availability = await siteAvailabilityChecker.CheckAllAsync(site, _cancellationToken);

        if (!availability.Success)
        {
            logger.LogError(
                "Site {Site} is unavailable: {Reason}",
                site,
                availability.ErrorMessage
            );

            await siteUnavailableNotifier.NotifyAsync(
                site,
                new HttpRequestException(availability.ErrorMessage ?? "Site is unavailable"),
                _cancellationToken
            );

            return;
        }

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
