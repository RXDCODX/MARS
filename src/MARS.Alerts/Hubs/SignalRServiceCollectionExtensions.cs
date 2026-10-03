using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Регистрация SignalR для хоста оверлея.
/// </summary>
/// <remarks>
/// Отдельным расширением, а не вызовом <c>AddSignalR()</c> прямо в <c>Program</c>,
/// потому что настройки ниже — не значения по умолчанию, а требования этого
/// оверлея, и их место должен объяснять владелец хоста.
/// </remarks>
public static class SignalRServiceCollectionExtensions
{
    /// <summary>
    /// Потолок входящего сообщения.
    /// </summary>
    /// <remarks>
    /// У SignalR по умолчанию 32 КБ, а оверлей присылает крупные объекты: списки
    /// призов, пачки сообщений чата, конфигурацию раскладки. Стандартного потолка
    /// не хватало бы, и клиент получал бы ошибку про размер вместо работающего
    /// экрана.
    /// </remarks>
    public const int MaximumReceiveMessageSize = 256 * 1024;

    public static IServiceCollection AddMarsSignalR(this IServiceCollection services)
    {
        services
            .AddSignalR(options => options.MaximumReceiveMessageSize = MaximumReceiveMessageSize)
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            );

        services.Configure<HubOptions>(options =>
        {
            // Пинг отправляется всем подключённым клиентам и ждёт ответа, поэтому
            // частый пинг — лишняя нагрузка при событиях.
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);

            // Обрыв у OBS обязан закрывать соединение быстро: иначе оверлей молча
            // перестаёт получать алерты, не показывая никакой ошибки.
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
