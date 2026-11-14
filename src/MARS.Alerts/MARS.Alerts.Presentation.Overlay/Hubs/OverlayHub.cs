using System.Threading.Tasks;
using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Modules.Alerts.Application.Adhd;
using MARS.Modules.Alerts.Application.Adhd.Models;
using MARS.Platform.Infrastructure.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

/// <summary>
/// Хаб оверлея. Единственная точка входа для браузера: всё, что раньше
/// доставлялось подпиской gRPC, приходит сюда тем же набором методов.
/// </summary>
/// <remarks>
/// <para>
/// Наследование от <see cref="Hub{T}"/> даёт <c>Clients</c> типизированным:
/// имя метода и его сигнатура берутся из <see cref="IOverlayHub"/>, поэтому
/// опечатка в строке невозможна в принципе.
/// </para>
/// <para>
/// Рассылка раскладки ADHD идёт через <see cref="IHubContext{THub,T}"/>, а не
/// через <c>Clients</c>: контекст подставляется контейнером, поэтому получатели
/// видны в тесте, а не только в живом стенде. Своего состояния у хаба нет
/// намеренно: набор подписчиков и очередь событий живут в
/// <c>EventBroadcaster</c>, откуда их читает <see cref="HubEventRelay"/> на
/// обслуживании хостов.
/// </para>
/// <para>
/// Клиентские вызовы (вызываемые браузером) живут методами самого хаба, а не
/// в <see cref="ITelegramusHub"/>: интерфейс объявляет только направление
/// сервер → клиент, и методы хаба, которых нет в интерфейсе, не попадают в
/// контракт <c>oneof</c>.
/// </para>
/// </remarks>
public class OverlayHub(
    ISoundMuteCoordinator soundMuteCoordinator,
    IAdhdLayoutService adhdLayout,
    IHubContext<OverlayHub, IOverlayHub> overlayClients,
    ILogger<OverlayHub> logger
) : Hub<IOverlayHub>
{
    /// <summary>
    /// Глушит звук на всех подписанных сервисах одновременно: микшер приложений
    /// (AudioController), TTS и плеер SoundRequest.
    /// </summary>
    /// <param name="excludedProcessNames">
    /// Список имён процессов-исключений. Пустой массив означает значения по
    /// умолчанию из конфигурации AudioController (OBS и его браузерные источники).
    /// </param>
    public async Task MuteAll(string[] excludedProcessNames)
    {
        await soundMuteCoordinator.MuteAsync(excludedProcessNames);
    }

    /// <summary>
    /// Снимает mute со всех сервисов, которые были заглушены через <see cref="MuteAll"/>.
    /// </summary>
    public async Task UnmuteSessions()
    {
        await soundMuteCoordinator.UnmuteAsync();
    }

    /// <summary>
    /// Сохраняет раскладку ADHD-экрана и рассылает её остальным оверлеям.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Панель раскладки живёт в браузере, а настройка — в базе этого сервиса.
    /// Пока вызов отсутствовал, панель перерисовывалась из локального состояния,
    /// показывала «сохранил», а после перезагрузки страницы всё возвращалось.
    /// </para>
    /// <para>
    /// Ответ — признак записи, а не переброшенная настройка: вызвавший её уже
    /// видит и так, а <see langword="false"/> говорит панели, что сохранять
    /// было некуда.
    /// </para>
    /// </remarks>
    public async Task<bool> UpdateAdhdConfig(AdhdLayoutConfigDto config)
    {
        var written = await adhdLayout.UpdateAsync(config, Context.ConnectionAborted);

        if (!written.Success || written.Result is null)
        {
            logger.LogWarning(
                "Раскладка ADHD не сохранена: {Error}",
                written.ErrorMessage ?? "неизвестная ошибка"
            );

            return false;
        }

        // Остальные оверлеи получают событие сразу, а не через широковещатель:
        // адресат — SignalR-подключения этого процесса, и лишний переход через
        // очередь реле только откладывал бы перерисовку. Отправитель исключён,
        // как и было у Clients.Others: он уже видит настройку у себя.
        await overlayClients
            .Clients.AllExcept(Context.ConnectionId)
            .AdhdConfig(AdhdConfigPayload.ToEvent(written.Result));

        logger.LogInformation(
            "Раскладка ADHD сохранена и разослана остальным оверлеям ({ConnectionId})",
            Context.ConnectionId
        );

        return true;
    }

    /// <summary>
    /// Отдаёт подключившемуся оверлею текущую раскладку.
    /// </summary>
    /// <remarks>
    /// В монолите это был вызов <c>GetCurrentConfig</c> при подписке. Без него
    /// свежезагруженная страница показывает набор виджетов по умолчанию, пока
    /// кто-нибудь не поменяет настройку, — то есть сохранённая раскладка
    /// выглядит потерянной.
    /// </remarks>
    public override async Task OnConnectedAsync()
    {
        var stored = await adhdLayout.GetAsync(Context.ConnectionAborted);

        if (stored.Success && stored.Result is not null)
        {
            await overlayClients
                .Clients.Client(Context.ConnectionId)
                .AdhdConfig(AdhdConfigPayload.ToEvent(stored.Result));
        }
        else
        {
            logger.LogWarning(
                "Раскладка ADHD не прочитана для подключения {ConnectionId}: {Error}",
                Context.ConnectionId,
                stored.ErrorMessage ?? "неизвестная ошибка"
            );
        }

        await base.OnConnectedAsync();
    }
}
