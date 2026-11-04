import { describe, expect, it, vi } from "vitest";

import { createHubConnection } from "./hubConnection";
import { createSignalRHubAdapter } from "./SignalRHubAdapter";

/**
 * Настоящий адаптер без сети: подменяется только `start` у соединения.
 *
 * Тест закрывает регрессию, которую не видели юнит-тесты на подделке: потребитель
 * стал забирать подписки через `acquire(handlers)` и звать `start()` без карты, а
 * `SignalRHubAdapter.connect` делал `Object.keys(handlers)` — и падал с
 * «Cannot convert undefined or null to object». На житом стенде это выглядело как
 * «хаб табло не подключается» на маршруте `/scoreboard-admin`, и поймал только
 * E2E.
 *
 * Здесь настоящий адаптер и настоящий `createHubConnection`, подменён ровно один
 * вызов — `start` у `HubConnection`.
 */
describe("настоящий адаптер хаба при подключении без карты", () => {
  /** Строит адаптер, у которого не запускается сеть, и возвращает его с остановкой. */
  const createOfflineAdapter = () => {
    const adapter = createSignalRHubAdapter("http://localhost:65535/hubs/test");
    const inner = adapter as unknown as {
      connection: { start: () => Promise<void>; stop: () => Promise<void> };
    };

    inner.connection.start = vi.fn(async () => undefined);
    inner.connection.stop = vi.fn(async () => undefined);

    return { adapter, inner };
  };

  it("открывает канал без обработчиков", async () => {
    const { adapter, inner } = createOfflineAdapter();

    // Ровно то, что делают пульт, видеоэкран и табло: подписки через acquire,
    // карты в start нет.
    await adapter.connect();

    expect(inner.connection.start).toHaveBeenCalledTimes(1);
  });

  it("открывает канал и с картой обработчиков", async () => {
    const { adapter, inner } = createOfflineAdapter();

    await adapter.connect({ Adhd: () => undefined } as never);

    expect(inner.connection.start).toHaveBeenCalledTimes(1);
  });

  it("не запускает соединение дважды при параллельном connect", async () => {
    // Гонка, найденная ревью: стор присваивал адаптер себе до await, и два
    // эффекта в одной пачке (ребёнок и родитель на маршруте /waifu) начинали
    // подключение параллельно. Второй `HubConnection.start()` отвергался с
    // «Cannot start a HubConnection that is not in the 'Disconnected' state»,
    // а catch в сторе обнулял адаптер, и все подписки документа оставались без
    // хаба при зелёном индикаторе.
    const { adapter, inner } = createOfflineAdapter();
    let releaseStart = () => undefined as void;

    inner.connection.start = vi.fn(
      () =>
        new Promise<void>(resolve => {
          releaseStart = () => resolve();
        })
    );

    const first = adapter.connect();
    const second = adapter.connect();

    releaseStart();
    await expect(first).resolves.toBeUndefined();
    await expect(second).resolves.toBeUndefined();

    // Старт канала ровно один, сколько бы вызывателей ни было.
    expect(inner.connection.start).toHaveBeenCalledTimes(1);
  });

  it("не считается подключённым после отключения", async () => {
    const { adapter, inner } = createOfflineAdapter();

    await adapter.connect();
    await adapter.disconnect();
    await adapter.connect();

    // Иначе тот же адаптер после stop() больше не поднялся бы: гвард на
    // «уже подключается» остался бы взведённым.
    expect(inner.connection.start).toHaveBeenCalledTimes(2);
  });

  it("выживает в сценарии соединения без карты", async () => {
    const { adapter, inner } = createOfflineAdapter();
    const connection = createHubConnection(() => adapter);

    // Ровно то, что делают пульт, видеоэкран и табло: подписки через acquire,
    // карты в start нет. Раньше здесь был TypeError.
    const release = connection.acquire({ Adhd: () => undefined });

    await expect(connection.start()).resolves.not.toBeNull();
    expect(inner.connection.start).toHaveBeenCalledTimes(1);

    release();
  });
});
