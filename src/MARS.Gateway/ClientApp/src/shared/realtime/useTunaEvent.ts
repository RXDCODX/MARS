import { useEffect } from "react";

import type { HubAdapter } from "./hubAdapter";
import type { HubConnection } from "./hubConnection";
import { tunaConnection } from "./hubConnections";
import { readBranch } from "./overlayPayload";

/**
 * Подписка на событие хаба информации о треке.
 *
 * Отдельный хук, а не ещё одна подписка в `useOverlayEvent`, потому что это
 * другой хаб: `/hubs/tuna` в `MARS.Alerts`, другой широковещатель на сервере и
 * своя подписка. Смешивать их в одной карте означало бы отправлять всем
 * подписчикам оба вида событий — оверлей получал бы треки, а экран трека —
 * алерты.
 *
 * Соединение открывает сам хук. Раньше `tunaConnection.start()` не звал никто:
 * адаптер клался в реестр только внутри `start`, поэтому `getTunaAdapter()`
 * всегда давал `null`, подписка выходила из эффекта, и события трека не доходили
 * ни до одного компонента. Признак на стенде — оверлей живой, а экран трека
 * молчит.
 *
 * Имя события не проверяется типами, в отличие от оверлейного хука, и это
 * единственное приведение типов в транспортном слое. Карта событий Tuna в
 * клиенте не описана, потому что на неё подписан один компонент. Когда
 * подписок станет несколько, карта появится вместе с обобщением адаптера по
 * карте событий.
 */
const TUNA_MUSIC_INFO = "TunaMusicInfo";

/** Форма события трека: содержимое ветки `oneof`. */
export interface TunaMusicEvent {
  data: unknown;
  hostname?: string;
  timestamp?: string;
}

/**
 * Разбирает событие трека.
 *
 * Сервер шлёт содержимое ветки `{ data, hostname, timestamp }`, а компонент
 * ждёт `TunaMusicDTO` с полем `data`. Ветка без `data` — это событие без
 * полезной нагрузки, и подписка должна его пропустить, а не отдать обработчик
 * с `undefined`.
 */
export function readTunaMusic(payload: unknown): TunaMusicEvent | null {
  const branch = readBranch(payload);

  if (branch === null || branch.data === undefined) {
    return null;
  }

  return branch as unknown as TunaMusicEvent;
}

export function useTunaEvent(
  handler: (music: TunaMusicEvent) => void,
  connection: HubConnection = tunaConnection
): void {
  useEffect(() => {
    const unsubscribeRef: { current: (() => void) | null } = {
      current: null,
    };

    // Сначала подписка на уже подключённый адаптер, потом открытие канала: так
    // событие, пришедшее между `start` и первым рендером, не потеряется. В
    // `connect` обработчики передаются целиком, но если адаптер уже поднят,
    // `start` их не примет — их надо цепить через `on`.
    const attach = (adapter: HubAdapter): void => {
      // Приведение локально и помечено: on() проверяет имя по карте событий
      // оверлейного хаба, а у хаба Tuna своя карта из одного имени.
      const subscribe = adapter.on.bind(adapter) as (
        event: string,
        handler: (payload: TunaMusicEvent) => void
      ) => () => void;

      unsubscribeRef.current = subscribe(TUNA_MUSIC_INFO, value => {
        const music = readTunaMusic(value);

        if (music !== null) {
          handler(music);
        }
      });
    };

    const alreadyConnected = connection.current();

    if (alreadyConnected !== null) {
      attach(alreadyConnected);
    }

    void connection
      .start()
      .then(adapter => {
        // Канал поднят после того, как мы проверили current(): подписка ещё не
        // цеплена, иначе первый же обработчик получил бы два события.
        if (unsubscribeRef.current === null) {
          attach(adapter);
        }
      })
      .catch(() => undefined);

    return () => {
      unsubscribeRef.current?.();
      unsubscribeRef.current = null;
    };
  }, [connection, handler]);
}
