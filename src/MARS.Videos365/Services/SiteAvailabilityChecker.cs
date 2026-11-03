using System.Net;
using MARS.Shared.Models;

namespace MARS.Videos365.Services;

/// <summary>
/// Проверка доступности сайта-источника перед обходом избранки: без неё
/// неотличимы «источник лежит» и «источник вернул пустую страницу», а второй
/// случай в монолите приводил к пометке видео «загружено», которого в канале нет.
/// </summary>
/// <remarks>
/// Результат возвращается, а не бросается: вызывающая сторона на ошибке отправляет
/// уведомление администраторам и корректно завершает проход.
/// </remarks>
public sealed class SiteAvailabilityChecker(
    IDnsResolver dnsResolver,
    IHttpClientFactory httpClientFactory,
    ILogger<SiteAvailabilityChecker> logger
)
{
    /// <summary>Проверяет, что хост резолвится хотя бы в один адрес.</summary>
    public async Task<OperationResult<IPAddress[]>> CheckDnsAsync(
        Uri site,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<IPAddress[]>.Fail("Стартовая ошибка проверки DNS");

        if (site is not null)
        {
            try
            {
                var addresses = await dnsResolver.GetHostAddressesAsync(
                    site.Host,
                    cancellationToken
                );

                if (addresses is { Length: > 0 })
                {
                    result = OperationResult<IPAddress[]>.Ok(addresses);
                }
                else
                {
                    result = OperationResult<IPAddress[]>.Fail($"DNS не разрешил хост {site.Host}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DNS resolution failed for {Host}", site.Host);
                result = OperationResult<IPAddress[]>.Fail(
                    $"Не удалось разрешить DNS для {site.Host}: {ex.Message}"
                );
            }
        }
        else
        {
            result = OperationResult<IPAddress[]>.Fail("Адрес сайта не задан");
        }

        return result;
    }

    /// <summary>Проверяет, что сайт отвечает успешным HTTP-статусом.</summary>
    public async Task<OperationResult> CheckPingPongAsync(
        Uri site,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult.Fail("Стартовая ошибка проверки сайта");

        if (site is not null)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(site, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    result = OperationResult.Ok();
                }
                else
                {
                    result = OperationResult.Fail(
                        $"Сайт {site} ответил кодом {(int)response.StatusCode}",
                        response.StatusCode
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Request to {Site} failed", site);
                result = OperationResult.Fail($"Сайт {site} недоступен: {ex.Message}");
            }
        }
        else
        {
            result = OperationResult.Fail("Адрес сайта не задан");
        }

        return result;
    }

    /// <summary>
    /// Полная проверка. Без успешного DNS HTTP-запрос не выполняется: источник
    /// не разрешается, и обход избранки всё равно вернёт пустую страницу.
    /// </summary>
    public async Task<OperationResult> CheckAllAsync(Uri site, CancellationToken cancellationToken)
    {
        var result = OperationResult.Fail("Стартовая ошибка проверки сайта");

        var dns = await CheckDnsAsync(site, cancellationToken);

        if (dns.Success)
        {
            result = await CheckPingPongAsync(site, cancellationToken);
        }
        else
        {
            result = OperationResult.Fail(dns.ErrorMessage ?? "Проверка DNS не пройдена");
        }

        return result;
    }
}
