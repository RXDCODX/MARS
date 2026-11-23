import { createHubConnection } from "./hubConnection";
import {
  createScoreboardHubAdapter,
  createSoundRequestHubAdapter,
} from "./SignalRHubAdapter";

/**
 * Подключения к хабам, кроме оверлейного.
 *
 * Оверлейное живёт в `overlayHubStore`: у него своя очередь алертов и своя
 * карта обработчиков, там же ему место. Остальным двум очередь не нужна, хватает
 * подписки компонента, поэтому у них здесь только жизненный цикл соединения.
 *
 * Пути разведены по разным адаптерам намеренно: один адаптер на два хаба означал
 * бы, что подписка табло едет в оверлейный сокет, где её никто не слушает.
 *
 * Раньше здесь ещё был реестр адаптера на каждый хаб. Он был write-only: его
 * писал `setScoreboardAdapter` и подобные, а не читал никто — остальные хабы берут
 * адаптер из `HubConnection.current()` напрямую. Плюс при закрытии соединения
 * в реестр никто не клал `null`, то есть будущий читатель получил бы мёртвый
 * адаптер. Единственный источник правды теперь сам `HubConnection`.
 */
export const scoreboardConnection = createHubConnection(
  createScoreboardHubAdapter
);

export const soundRequestConnection = createHubConnection(
  createSoundRequestHubAdapter
);

