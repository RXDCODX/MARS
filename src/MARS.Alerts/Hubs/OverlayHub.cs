using System.Threading.Tasks;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Alerts.Services.Audio;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Хаб оверлея. Единственная точка входа для браузера: всё, что раньше
/// доставлялось подпиской gRPC, приходит сюда тем же набором методов.
/// </summary>
/// <remarks>
/// <para>
/// Наследование от <see cref="Hub{T}"/> даёт <c>Clients.Client(...)</c>
/// типизированным: имя метода и его сигнатура берутся из
/// <see cref="IOverlayHub"/>, поэтому опечатка в строке невозможна в принципе.
/// </para>
/// <para>
/// Своего состояния у хаба нет намеренно. Набор подписчиков и очередь событий
/// живут в <c>GrpcEventBroadcaster</c>, откуда их читает <see cref="HubEventRelay"/>
/// на обслуживании хостов. Если бы подписки учитывались здесь, то при двух
/// подключённых оверлеях и перезапуске фоновой задачи состояние разъехалось
/// бы с тем, что видят клиенты.
/// </para>
/// <para>
/// Клиентские вызовы (вызываемые браузером) живут методами самого хаба, а не
/// в <see cref="ITelegramusHub"/>: интерфейс объявляет только направление
/// сервер → клиент, и методы хаба, которых нет в интерфейсе, не попадают в
/// контракт <c>oneof</c>.
/// </para>
/// </remarks>
public class OverlayHub(SoundMuteCoordinator soundMuteCoordinator) : Hub<IOverlayHub>
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
}
