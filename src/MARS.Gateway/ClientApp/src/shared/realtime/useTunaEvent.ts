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
 *
 * Прогресс приводится к имени, под которым его читают компоненты. В
 * REST-контракте это `progress`, а в `tuna.proto` — `progression`, и хаб
 * отдаёт proto. Без приведения `track.progress` был `undefined`, и полоса
 * прогресса начинала с нуля вместо фактической позиции трека.
 */
export function readTunaMusic(payload: unknown): TunaMusicEvent | null {
  const branch = readBranch(payload);

  if (branch === null || branch.data === undefined) {
    return null;
  }

  return {
    ...branch,
    data: normalizeTrackProgress(branch.data),
  } as unknown as TunaMusicEvent;
}

/**
 * Добавляет `progress` треку, если на проводе поле называется `progression`.
 *
 * Приведение без потерь: оба имени живут в одном треке, и REST-форма приходит
 * без `progression`, поэтому дописывается только недостающее. Нулевой прогресс
 * остаётся нулём — «позиция неизвестна» и «трек в начале» — разные вещи.
 */
function normalizeTrackProgress(data: unknown): unknown {
  if (data === null || typeof data !== "object" || Array.isArray(data)) {
    return data;
  }

  const track = data as Record<string, unknown>;
  const normalized: Record<string, unknown> = { ...track };

  // `progression` → `progress`: имя из proto против имени в REST-контракте.
  // Дописывается только недостающее, поэтому уже приведённый трек не меняется.
  if (normalized.progress === undefined && track.progression !== undefined) {
    normalized.progress = track.progression;
  }

  // `albumUrl` → `album_url`: то же расхождение второго поля. Пока оно не
  // читается, но подпись «как на проводе» была бы неправдивой.
  if (normalized.album_url === undefined && track.albumUrl !== undefined) {
    normalized.album_url = track.albumUrl;
  }

  return normalized;
}

export function useTunaEvent(
  handler: (music: TunaMusicEvent) => void,
  connection: HubConnection = tunaConnection
): void {
  useEffect(() => {
    const unsubscribeRef: { current: (() => void) | null } = {
      current: null,
    };

    // Размонтирование отмечается отдельным флагом: значение `unsubscribeRef`
    // одинаково у живого и у мёртвого эффекта.
    let disposed = false;

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
        //
        // Флаг размонтирования обязателен: у живого и у мёртвого эффекта
        // unsubscribeRef равен null, и без отдельной проверки обработчик цеплялся
        // бы на размонтированном компоненте, а снимать его было бы уже некому.
        // Каждый уход со страницы во время рукопожатия добавлял бы вечного
        // подписчика TunaMusicInfo, и разбор трека шёл бы до конца жизни
        // документа.
        if (!disposed && unsubscribeRef.current === null) {
          attach(adapter);
        }
      })
      .catch(() => undefined);

    return () => {
      disposed = true;
      unsubscribeRef.current?.();
      unsubscribeRef.current = null;
    };
  }, [connection, handler]);
}
