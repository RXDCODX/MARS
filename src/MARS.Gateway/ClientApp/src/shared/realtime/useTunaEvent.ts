import { useEffect } from "react";

import type { HubAdapter } from "./hubAdapter";
import { getOverlayAdapter } from "./overlayHub";
import { readBranch } from "./overlayPayload";

/**
 * Подписка на событие хаба информации о треке.
 *
 * Отдельный хук, а не ещё одна подписка в `useOverlayEvent`, потому что это
 * другой хаб: `/hubs/tuna` в `MARS.Alerts`, другой широковещатель на сервере
 * и своя подписка. Смешивать их в одной карте означало бы отправлять всем
 * подписчикам оба вида событий — оверлей получал бы треки, а экран трека —
 * алерты.
 *
 * Имя события не проверяется типами, в отличие от оверлейного хука, и это
 * единственное приведение типов в транспортном слое. Оно помечено и
 * локально: карта событий Tuna в клиенте не описана, потому что на неё
 * подписан один компонент. Когда подписок станет несколько, карта
 * появится вместе с обобщением адаптера по карте событий.
 */
const TUNA_MUSIC_INFO = "TunaMusicInfo";

/** Форма события трека: ветка `oneof` с полем `info`. */
export interface TunaMusicEvent {
  data: unknown;
  hostname?: string;
  timestamp?: string;
}

/**
 * Разбирает ветку `oneof` события трека.
 *
 * Сервер шлёт `{ info: { data, hostname, timestamp } }`, а компонент ждёт
 * `TunaMusicDTO` с полем `data`. Форма `TunaPayload` повторяет поля DTO, так
 * что разбор сводится к выбору ветки.
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
  adapter: HubAdapter | null = getOverlayAdapter()
): void {
  useEffect(() => {
    if (adapter === null) {
      return undefined;
    }

    // Приведение локально и помечено: on() проверяет имя по карте событий
    // оверлейного хаба, а у хаба Tuna своя карта из одного имени.
    const subscribe = adapter.on.bind(adapter) as (
      event: string,
      handler: (music: TunaMusicEvent) => void
    ) => () => void;

    return subscribe(TUNA_MUSIC_INFO, handler);
  }, [adapter, handler]);
}
