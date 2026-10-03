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
  acquire: (handlers?: Record<string, (payload: never) => void>) => () => void;

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
   * Карты обработчиков владельцев, ещё не подцепленные к адаптеру.
   *
   * Нужно из-за порядка: `acquire` с обработчиками вызывают обычно раньше, чем
   * кто-то открыл канал, — так работает любой хук. Если в этот момент адаптера
   * нет, обработчики некуда цеплять, а молча выбросить их нельзя: событие просто
   * не пришло бы никогда, и это выглядело бы как «хаб подключён, всё тихо».
   */
  const unattached: {
    handlers?: Record<string, (payload: never) => void>;
    detach: (() => void)[] | null;
  }[] = [];

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
  ): (() => void)[] => {
    if (handlers === undefined) {
      return [];
    }

    // Регистрация через `on`, а не через карту `connect`: переподключать уже
    // открытый канал нельзя, а вот дописать подписчика — можно, и настоящий
    // транспорт так же принимает `on` после старта.
    //
    // Отписки возвращаются и хранятся у владельца. Раньше они выбрасывались, и
    // каждый вход на страницу с хабом добавлял ещё один обработчик на уже
    // подключённый адаптер: событие обрабатывалось N раз, а список замыканий
    // размонтированных хуков рос без ограничений.
    return Object.entries(handlers).map(([event, handler]) =>
      (
        adapter.on as unknown as (
          event: string,
          handler: (payload: unknown) => void
        ) => () => void
      )(event, handler as unknown as (payload: unknown) => void)
    );
  };

  /**
   * Цепляет карты, отданные владельцами до подключения.
   *
   * Вызывается сразу после того, как адаптер стал текущим, — то есть до того,
   * как `start` разошлётся вызывающим. Иначе событие успело бы прийти раньше
   * подписки, и первый ответ сервера потерялся бы.
   */
  const attachPending = (adapter: HubAdapter): void => {
    for (const entry of [...unattached]) {
      entry.detach = attach(adapter, entry.handlers);
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

    // Подписки снятого соединения больше нечего держать: адаптер закрыт, и его
    // карта недостижима. Отписки потребителей при этом остаются действительными
    // до их вызова — они снимают обработчики у уже закрытого адаптера, что
    // безвредно и не даёт счётчику уйти в минус.
    unattached.length = 0;

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
          attachPending(adapter);

          return adapter;
        })
        .finally(() => {
          pending = null;
        });

      return pending;
    },

    acquire: (handlers?: Record<string, (payload: never) => void>) => {
      owners += 1;
      let released = false;

      const entry = { handlers, detach: null as (() => void)[] | null };

      if (connected !== null && handlers !== undefined) {
        entry.detach = attach(connected, handlers);
      } else if (handlers !== undefined) {
        // Канала ещё нет: карта ждёт подключения и цепляется сама в `start`.
        unattached.push(entry);
      }

      return () => {
        // Отписка должна быть идемпотентной: StrictMode и повторный cleanup
        // вызывают её дважды, а счётчик не должен уйти в минус и закрыть
        // соединение у живого потребителя.
        if (released) {
          return;
        }

        released = true;
        owners -= 1;

        const detach = entry.detach ?? [];
        entry.detach = null;
        unattached.splice(unattached.indexOf(entry), 1);

        for (const unsubscribe of detach) {
          unsubscribe();
        }

        if (owners === 0 && connected !== null) {
          void stop();
        }
      };
    },

    stop,

    current: () => connected,
  };
}
