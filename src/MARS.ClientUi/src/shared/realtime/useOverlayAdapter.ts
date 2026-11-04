import { useSyncExternalStore } from "react";

import { getOverlayAdapter, subscribeToOverlayAdapter } from "./overlayHub";

/**
 * Текущий адаптер оверлейного хаба для компонента.
 *
 * Отдельный маленький хук, потому что и подписка на событие, и вызов метода
 * должны видеть один и тот же адаптер. Дублировать чтение реестра в двух местах
 * означало бы риск разойтись при следующей правке.
 *
 * Значение наблюдаемо: смена адаптера перерисовывает компонент, поэтому
 * подписка, взятая до подключения, не остаётся мёртвой.
 */
export function useOverlayAdapter() {
  return useSyncExternalStore(
    subscribeToOverlayAdapter,
    getOverlayAdapter,
    getOverlayAdapter
  );
}
