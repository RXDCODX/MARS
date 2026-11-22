using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

// TODO: производитель призов отсутствует с миграции — в монолите их отправлял
// TelegramusHub.OnConnectedAsync при подключении оверлея. Восстановить отправку
// (AlertsHub.OnConnectedAsync / листенер WaifuGacha) или удалить ветви вместе с
// фронтенд-подписками. Пока ветки UpdateWaifuPrizes/UpdateFumoPrizes/
// UpdateFrogPrizes/UpdateMikuPrizes остаются в конверте и IAlertsHub.
public class PrizesEvent : OverlayEventAbstract {
    public string PrizesJson { get; set; } = "";
}
