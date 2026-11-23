using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Kernel;
using SignalRSwaggerGen.Attributes;
using SignalRSwaggerGen.Enums;

namespace MARS.Alerts.Models.Overlay;

/// <summary>
/// Методы оверлейного хаба, которые сервер шлёт подписчикам.
/// </summary>
/// <remarks>
/// <para>
/// Интерфейс объявляет только направление сервер → клиент. Вызовы клиента
/// (пауза, смена сцены, отчёт о воспроизведении) живут методами самой
/// <c>AlertsHub</c>: клиентские аргументы и ответы не имеют оверлейного
/// payload'а и в <c>oneof</c> не попадают.
/// </para>
/// <para>
/// Каждый метод соответствует одной ветке события в манифесте
/// <c>overlay-hub.manifest.json</c> (исторически — ветки <c>oneof</c> в
/// <c>telegramus.proto</c>, которого в репозитории больше нет). Имя метода —
/// строка на проводе, поэтому список имён продублирован в манифесте, а
/// расхождение ловят тесты.
/// </para>
/// <para>
/// Payload'ы — сгенерированные из proto сообщения, а не <c>object</c>, как в
/// предшественнике. С <c>object</c> клиент получал <c>any</c> и не мог ни
/// проверить форму события при разработке, ни описать его в моках.
/// </para>
/// </remarks>
[SignalRHub("/hubs/overlay", AutoDiscover.MethodsAndParams, tag: "OverlayHub")]
public interface IAlertsHub
{
    /// <summary>Алерт с одним медиа. Реакция на награду.</summary>
    Task Alert(AlertEvent alert);

    /// <summary>Пакет алертов. Реакция на несколько наград одним вбросом.</summary>
    Task Alerts(AlertsEvent alerts);

    /// <summary>Ролл вайфу.</summary>
    Task WaifuRoll(WaifuRollEvent waifuRoll);

    /// <summary>Новый вайфу добавлен в коллекцию.</summary>
    Task AddNewWaifu(AddNewWaifuEvent addNewWaifu);

    /// <summary>Показать текущую жену.</summary>
    Task ShowCurrentWife(ShowCurrentWifeEvent showCurrentWife);

    /// <summary>Слияние двух вайфу.</summary>
    Task MergeWaifu(MergeWaifuEvent mergeWaifu);

    /// <summary>Пятничный фумо-алерт.</summary>
    Task FumoFriday(FumoFridayEvent fumoFriday);

    /// <summary>Новое сообщение чата.</summary>
    Task NewMessage(NewMessageEvent newMessage);

    /// <summary>Сообщение чата удалено.</summary>
    Task DeleteMessage(DeleteMessageEvent deleteMessage);

    /// <summary>Частицы на экране.</summary>
    Task MakeScreenParticles(ScreenParticlesEvent makeScreenParticles);

    /// <summary>Эмодзи-частицы на экране.</summary>
    Task MakeScreenEmojisParticles(ScreenEmojisParticlesEvent makeScreenEmojisParticles);

    /// <summary>Случайная картинка. По форме совпадает с <see cref="Alert"/>.</summary>
    Task RandomMem(AlertEvent randomMem);

    /// <summary>Режим ADHD на заданное число секунд.</summary>
    Task Adhd(AdhdEvent adhd);

    /// <summary>Гао-алерт.</summary>
    Task GaoAlert(GaoAlertEvent gaoAlert);

    /// <summary>Титры. Payload пустой.</summary>
    Task Credits();

    /// <summary>Майкл Джексон. Payload пустой.</summary>
    Task MichaelJackson();

    /// <summary>Мику Минди.</summary>
    Task MikuMonday(MikuMondayEvent mikuMonday);

    /// <summary>Луч Мику Мику.</summary>
    Task MikuMikuBeam(MikuMikuBeamEvent mikuMikuBeam);

    /// <summary>Фонк-правка. Payload пустой.</summary>
    Task PhonkEdit();

    /// <summary>Правка TikTok.</summary>
    Task TikTokEdit(TikTokEditEvent tikTokEdit);

    /// <summary>Полный возврат.</summary>
    Task AllRefund(AllRefundEvent allRefund);

    /// <summary>Фумо-ролл.</summary>
    Task FumoRoll(FumoRollEvent fumoRoll);

    /// <summary>Ролл жабы.</summary>
    Task FrogRoll(FrogRollEvent frogRoll);

    /// <summary>Ролл мику.</summary>
    Task MikuRoll(MikuRollEvent mikuRoll);

    /// <summary>Конфигурация раскладки ADHD изменилась.</summary>
    Task AdhdConfig(AdhdConfigEvent adhdConfig);
}
