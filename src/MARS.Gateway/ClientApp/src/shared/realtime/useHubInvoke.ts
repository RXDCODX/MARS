import { useCallback } from "react";

import type { HubAdapter, HubInvocationMap } from "./hubAdapter";
import { useOverlayAdapter } from "./useOverlayAdapter";

/**
 * Вызов клиентского метода хаба из компонента.
 *
 * Раньше это был `SignalRContext.invoke("MuteAll", [])` — строка без проверки,
 * и опечатка проходила компиляцию. Здесь имя берётся из карты
 * `HubInvocationMap`, поэтому `invoke("MuteAl")` не собирается.
 *
 * Адаптер берётся из того же реестра, что и у `useOverlayEvent`: оба хука
 * смотрят на один источник, иначе подписка и вызов разошлись бы по разным
 * соединениям.
 *
 * Пока хаб не подключён, вызов не делается и ошибки не бросается. Раньше вызов
 * без соединения падал, и компонент оверлея — а это фон, чистый фон поверх
 * видео — падал вместе с ним. Отсутствие канала на экране безопасно: событие
 * отправки всё равно никому не дошло бы.
 */
export function useHubInvoke(): <K extends keyof HubInvocationMap>(
  method: K,
  ...args: HubInvocationMap[K]
) => Promise<void> {
  const adapter = useOverlayAdapter();

  return useCallback(
    async (method, ...args) => {
      if (adapter === null) {
        return;
      }

      await (adapter as HubAdapter).invoke(method, ...args);
    },
    [adapter]
  );
}
