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
   * их регистрировать нельзя, иначе событие легло бы в очередь дважды. Обработчики
   * позднего подписчика долетают через `on`, см. `attach` ниже.
   */
  start: (
    handlers?: Record<string, (payload: never) => void>
  ) => Promise<HubAdapter>;

  /**
   * Забирает владение соединением и возвращает отписку от владения.
   *
   * На один хаб соединение одно, а потребителей несколько: пульт и видеоэкран
   * слушают один сокет очереди звуковых запросов. Раньше размонтирование одного
   * закрывало соединение у другого, и тот оставался с мёртвым адаптером при
   * живом на вид плеере.
   *
   * Отписка безопасна в любом порядке и многократно: StrictMode вызывает
   * эффекты дважды, то есть пара acquire/release может пройти вхолостую.
   */
  acquire: () => () => void;

  /** Отключается и забывает адаптер. Повторный вызов безопасен. */
  stop: () => Promise<void>;
  /** Подключённый адаптер или `null`. */
  current: () => HubAdapter | null;
};

export function createHubConnection(create: () => HubAdapter): HubConnection {
  let connected: HubAdapter | null = null;
  let pending: Promise<HubAdapter> | null = null;
  let owners = 0;

  /**
   * Вешает обработчики на уже подключённый адаптер.
   *
   * Отдельный путь нужен из-за реального отказа: соединение общее на хаб, а
   * подписчиков несколько, и поздний приходит вторым. Раньше `start` на
   * подключённом соединении просто возвращал адаптер, не регистрируя ничего,
   * — второй подписчик молча терял свои события при полностью «зелёном»
   * подключении. Теперь его обработчики цепляются к тому же адаптеру.
   */
  const attach = (
    adapter: HubAdapter,
    handlers?: Record<string, (payload: never) => void>
  ): void => {
    if (handlers === undefined) {
      return;
    }

    // Регистрация через `on`, а не через карту `connect`: переподключать уже
    // открытый канал нельзя, а вот дописать подписчика — можно, и настоящий
    // транспорт так же принимает `on` после старта.
    for (const [event, handler] of Object.entries(handlers)) {
      (
        adapter.on as unknown as (
          event: string,
          handler: (payload: unknown) => void
        ) => () => void
      )(event, handler as unknown as (payload: unknown) => void);
    }
  };

  /**
   * Закрывает соединение и сбрасывает владение.
   *
   * Отдельной функцией, а не методом литерала: `acquire` вызывает её из своей
   * отписки, а в литерале имя ещё не объявлено.
   */
  const stop = async (): Promise<void> => {
    const adapter = connected;
    connected = null;
    owners = 0;

    if (adapter !== null) {
      await adapter.disconnect();
    }
  };

  return {
    start: async (handlers?: Record<string, (payload: never) => void>) => {
      if (connected !== null) {
        attach(connected, handlers);

        return connected;
      }

      if (pending !== null) {
        const adapter = await pending;
        attach(adapter, handlers);

        return adapter;
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

    acquire: () => {
      owners += 1;
      let released = false;

      return () => {
        // Отписка должна быть идемпотентной: StrictMode и повторный cleanup
        // вызывают её дважды, а счётчик не должен уйти в минус и закрыть
        // соединение у живого потребителя.
        if (released) {
          return;
        }

        released = true;
        owners -= 1;

        if (owners === 0 && connected !== null) {
          void stop();
        }
      };
    },

    stop,

    current: () => connected,
  };
}
