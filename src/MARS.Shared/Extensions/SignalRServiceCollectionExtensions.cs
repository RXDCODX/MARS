using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Extensions;

/// <summary>
/// Регистрация SignalR для сервисов, которые держат хабы.
/// </summary>
/// <remarks>
/// <para>
/// Живёт в <c>MARS.Shared</c>, а не в <c>MARS.Alerts</c>, где был раньше.
/// Правило нужно трём сервисам — <c>MARS.Alerts</c>, <c>MARS.Scoreboard</c> и
/// <c>MARS.SoundRequest</c>, — и ссылаться на <c>MARS.Alerts</c> из них нельзя:
/// это проектные ссылки, которые тянут за собой второй <c>appsettings.json</c>
/// и роняют publish (NETSDK1152). Пока настройка жила в <c>Alerts</c>,
/// <c>Scoreboard</c> и <c>SoundRequest</c> звали сырой <c>AddSignalR()</c> и
/// получали другой формат на том же клиенте.
/// </para>
/// <para>
/// <c>AddMarsDefaults</c> зовут все сервисы, и правило, о котором забывают в
/// одном из них, разошлось бы ровно так же, как разошёлся бы забытый
/// <c>AddControllers</c>.
/// </para>
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
            .AddJsonProtocol(ConfigureJsonProtocol);

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

    /// <summary>
    /// Формат JSON на проводе хабов.
    /// </summary>
    /// <remarks>
    /// Отдельным методом, а не только внутри <c>AddMarsSignalR</c>, чтобы
    /// правило можно было проверить напрямую: собирать контейнер ради одного
    /// утверждения дорого, а проверять тут нечего — <c>PayloadSerializerOptions</c>
    /// и есть весь контракт.
    /// <para>
    /// Перечисления обязаны ехать именами. Клиент сравнивает
    /// <c>playerState.state === PlayerStateStateEnum.Playing</c>; при числах
    /// сравнение не сходилось никогда, и экран видео оставался в паузе при
    /// играющем плеере. Настройка <c>JsonStringEnumConverter</c> в
    /// <c>AddMarsDefaults</c> на хабы не действует: это опции MVC, у SignalR
    /// свой сериализатор.
    /// </para>
    /// </remarks>
    public static void ConfigureJsonProtocol(JsonHubProtocolOptions options)
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }
}
