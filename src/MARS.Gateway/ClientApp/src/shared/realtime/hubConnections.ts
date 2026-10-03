import { createHubConnection } from "./hubConnection";
import { setScoreboardAdapter } from "./scoreboardHub";
import {
  createScoreboardHubAdapter,
  createSoundRequestHubAdapter,
  createTunaHubAdapter,
} from "./SignalRHubAdapter";
import { setSoundRequestAdapter } from "./soundRequestHub";
import { setTunaAdapter } from "./tunaHub";

/**
 * Подключения к хабам, кроме оверлейного.
 *
 * Оверлейное живёт в `telegramusHubStore`: у него своя очередь алертов и своя
 * карта обработчиков, там же ему место. Остальным трём очередь не нужна, хватает
 * подписки компонента, поэтому у них здесь только жизненный цикл соединения.
 *
 * Пути и реестры разведены по разным модулям намеренно: один адаптер на два хаба
 * означал бы, что подписка табло едет в оверлейный сокет, где её никто не слушает.
 */
export const tunaConnection = createHubConnection(() => {
  const adapter = createTunaHubAdapter();
  setTunaAdapter(adapter);

  return adapter;
});

export const scoreboardConnection = createHubConnection(() => {
  const adapter = createScoreboardHubAdapter();
  setScoreboardAdapter(adapter);

  return adapter;
});

export const soundRequestConnection = createHubConnection(() => {
  const adapter = createSoundRequestHubAdapter();
  setSoundRequestAdapter(adapter);

  return adapter;
});
