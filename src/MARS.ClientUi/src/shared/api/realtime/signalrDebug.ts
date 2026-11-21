/**
 * Единая точка управления прозрачностью логов SignalR в браузере.
 *
 * По умолчанию логи включены в dev/test (import.meta.env.DEV) и выключены
 * в production-сборке. Форсировать можно через VITE_SIGNALR_DEBUG=true|false
 * в .env.* — это полезно, когда нужно отлаживать стенд/прод без пересборки
 * с dev-флагом.
 *
 * Логи не фильтруются по уровню: при включённом debug печатается всё, что
 * эмитит сам клиент SignalR (negotiate, транспорт, handshake, ошибки) плюс
 * наши явные сообщения о попытках и событиях.
 */
export function signalrDebugEnabled(): boolean {
  const override = import.meta.env.VITE_SIGNALR_DEBUG;

  if (override === "true") {
    return true;
  }

  if (override === "false") {
    return false;
  }

  return Boolean(import.meta.env.DEV);
}

export const SIGNALR_LOG_PREFIX = "[SignalR]";
