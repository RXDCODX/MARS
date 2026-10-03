using MARS.Shared.Grpc.Telegramus;

namespace MARS.Alerts.Hubs.Interfaces;

/// <summary>
/// Методы оверлейного хаба, которые сервер шлёт подписчикам.
/// </summary>
/// <remarks>
/// <para>
/// Интерфейс объявляет только направление сервер → клиент. Вызовы клиента
/// (пауза, смена сцены, отчёт о воспроизведении) живут методами самой
/// <c>OverlayHub</c>: клиентские аргументы и ответы не имеют оверлейного
/// payload'а и в <c>oneof</c> не попадают.
/// </para>
/// <para>
/// Каждый метод соответствует ровно одной ветке <c>oneof event</c> в
/// <c>telegramus.proto</c>. Имя метода — строка на проводе, поэтому список
/// имён продублирован в <c>overlay-hub.manifest.json</c>, а расхождение
/// ловят тесты.
/// </para>
/// <para>
/// Payload'ы — сгенерированные из proto сообщения, а не <c>object</c>, как в
/// предшественнике. С <c>object</c> клиент получал <c>any</c> и не мог ни
/// проверить форму события при разработке, ни описать его в моках.
/// </para>
/// </remarks>
public interface ITelegramusHub
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

    /// <summary>Призы за вайфу обновились.</summary>
    Task UpdateWaifuPrizes(PrizesEvent updateWaifuPrizes);

    /// <summary>Пятничный фумо-алерт.</summary>
    Task FumoFriday(FumoFridayEvent fumoFriday);

    /// <summary>Новое сообщение чата.</summary>
    Task NewMessage(NewMessageEvent newMessage);

    /// <summary>Сообщение чата удалено.</summary>
    Task DeleteMessage(DeleteMessageEvent deleteMessage);

    /// <summary>Подсветка сообщения.</summary>
    Task Highlite(HighliteEvent highlite);

    /// <summary>Данные клиента Twitch переданы оверлею.</summary>
    Task PostTwitchInfo(PostTwitchInfoEvent postTwitchInfo);

    /// <summary>Частицы на экране.</summary>
    Task MakeScreenParticles(ScreenParticlesEvent makeScreenParticles);

    /// <summary>Эмодзи-частицы на экране.</summary>
    Task MakeScreenEmojisParticles(ScreenEmojisParticlesEvent makeScreenEmojisParticles);

    /// <summary>Случайная картинка. По форме совпадает с <see cref="Alert"/>.</summary>
    Task RandomMem(AlertEvent randomMem);

    /// <summary>Автосообщение.</summary>
    Task AutoMessage(AutoMessageEvent autoMessage);

    /// <summary>Режим ADHD на заданное число секунд.</summary>
    Task Adhd(AdhdEvent adhd);

    /// <summary>Взрыв. Payload пустой.</summary>
    Task Explosion();

    /// <summary>Алерт Лероя. Payload пустой.</summary>
    Task LeroyAlert();

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

    /// <summary>Начало раунда аудиоквиза.</summary>
    Task AudioQuizStart(AudioQuizStartEvent audioQuizStart);

    /// <summary>Конец аудиоквиза. Payload пустой.</summary>
    Task AudioQuizStop();

    /// <summary>Фумо-ролл.</summary>
    Task FumoRoll(FumoRollEvent fumoRoll);

    /// <summary>Призы за фумо обновились.</summary>
    Task UpdateFumoPrizes(PrizesEvent updateFumoPrizes);

    /// <summary>Ролл жабы.</summary>
    Task FrogRoll(FrogRollEvent frogRoll);

    /// <summary>Призы за жабу обновились.</summary>
    Task UpdateFrogPrizes(PrizesEvent updateFrogPrizes);

    /// <summary>Ролл мику.</summary>
    Task MikuRoll(MikuRollEvent mikuRoll);

    /// <summary>Призы за мику обновились.</summary>
    Task UpdateMikuPrizes(PrizesEvent updateMikuPrizes);

    /// <summary>Конфигурация раскладки ADHD изменилась.</summary>
    Task AdhdConfig(AdhdConfigEvent adhdConfig);
}
