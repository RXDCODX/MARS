import type { HubAdapter } from "./hubAdapter";

/**
 * Подключение к одному хабу: адаптер, его жизненный цикл и идемпотентный старт.
 *
 * Раньше каждый стор и хук строил `HubConnection` сам, и получалось четыре
 * копии одного и того же: `connection` в состоянии zustand (из-за чего подделку
 * положить было некуда) плюс ручной `start`, который при повторном вызове
 * создавал второе соединение. StrictMode вызывает эффекты дважды, так что
 * второе соединение было не гипотезой.
 *
 * Здесь адаптер лежит в замыкании, `start` переиспользует уже подключённый, а
 * параллельные вызовы ждут одно и то же обещание: иначе два компонента,
 * смонтированные одновременно, открыли бы два сокета на один хаб.
 */
export type HubConnection = {
  /**
   * Подключается, если ещё не подключён. Повторный вызов не создаёт второе
   * соединение, а возвращает то же обещание.
   *
   * Обработчики передаются только при первом вызове: на уже подключённом адаптере
   * их регистрировать нельзя, иначе событие легло бы в очередь дважды.
   */
  start: (
    handlers?: Record<string, (payload: never) => void>
  ) => Promise<HubAdapter>;
  /** Отключается и забывает адаптер. Повторный вызов безопасен. */
  stop: () => Promise<void>;
  /** Подключённый адаптер или `null`. */
  current: () => HubAdapter | null;
};

export function createHubConnection(create: () => HubAdapter): HubConnection {
  let connected: HubAdapter | null = null;
  let pending: Promise<HubAdapter> | null = null;

  return {
    start: async (handlers?: Record<string, (payload: never) => void>) => {
      if (connected !== null) {
        return connected;
      }

      if (pending !== null) {
        return pending;
      }

      const adapter = create();

      pending = adapter
        .connect(handlers as never)
        .then(() => {
          connected = adapter;

          return adapter;
        })
        .finally(() => {
          pending = null;
        });

      return pending;
    },

    stop: async () => {
      const adapter = connected;
      connected = null;

      if (adapter !== null) {
        await adapter.disconnect();
      }
    },

    current: () => connected,
  };
}
