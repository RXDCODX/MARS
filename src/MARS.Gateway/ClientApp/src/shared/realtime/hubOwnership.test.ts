import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { createHubConnection } from "./hubConnection";

/**
 * Владение соединением между несколькими потребителями.
 *
 * На хаб очереди звуковых запросов один сокет на документ, а потребителей два:
 * пульт и видеоэкран. Раньше размонтирование видеоэкрана вызывало `stop()` и
 * закрывало соединение у пульта, оставляя его с мёртвым адаптером: `SkipTrack` и
 * `FrontStateChange` откатывались с «connection is disconnected», а
 * `PlayerStateChange` переставали приходить — при живом на вид плеере.
 */
describe("владение соединением между потребителями", () => {
  it("соединение живёт, пока жив хотя бы один потребитель", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const releaseFirst = connection.acquire();
    await connection.start();

    const releaseSecond = connection.acquire();

    // Первый потребитель уходит: второй ещё держит соединение.
    releaseFirst();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(0);

    releaseSecond();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("размонтирование в StrictMode не закрывает соединение", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    // Два настоящих потребителя: пульт и видеоэкран.
    const releasePlayer = connection.acquire();
    const releaseScreen = connection.acquire();
    await connection.start();

    // StrictMode отрабатывает эффект и его cleanup второй раз. Если бы каждая
    // такая пара уменьшала счётчик на единицу, соединение закрылось бы, пока
    // оба потребителя ещё на экране.
    const transientPlayer = connection.acquire();
    const transientScreen = connection.acquire();
    transientPlayer();
    transientScreen();

    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(0);

    releasePlayer();
    await Promise.resolve();
    // Один потребитель ещё жив.
    expect(adapter.disconnectCalls).toBe(0);

    releaseScreen();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("повторная отписка не закрывает соединение у живого потребителя", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const releaseFirst = connection.acquire();
    const releaseSecond = connection.acquire();
    await connection.start();

    // Cleanup может прийти дважды. Идемпотентная отписка обязана не съесть
    // чужое владение, иначе соединение закрылось бы под работающим потребителем.
    releaseFirst();
    releaseFirst();
    await Promise.resolve();

    expect(adapter.disconnectCalls).toBe(0);

    releaseSecond();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отписка до старта не закрывает уже поднятое соединение", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();

    // Порядок в StrictMode бывает обратным: отписка приходит раньше старта.
    release();
    await connection.start();

    // Отписка уже отработала и повторять её нельзя: соединение остаётся живым,
    // иначе потребитель остался бы без канала без единой ошибки.
    expect(adapter.disconnectCalls).toBe(0);
    expect(connection.current()).toBe(adapter);
  });

  it("повторный acquire не закрывает соединение раньше времени", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    await connection.start();

    const releases = [
      connection.acquire(),
      connection.acquire(),
      connection.acquire(),
    ];

    releases.forEach(release => release());
    await Promise.resolve();

    // Три потребителя отпустили — соединение закрылось один раз.
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отсутствие потребителей означает, что старт можно повторить", async () => {
    const adapters: FakeHubAdapter[] = [];
    const connection = createHubConnection(() => {
      const adapter = new FakeHubAdapter();
      adapters.push(adapter);

      return adapter;
    });

    const release = connection.acquire();
    await connection.start();
    release();
    await Promise.resolve();

    await connection.start();

    expect(adapters).toHaveLength(2);
    expect(connection.current()).toBe(adapters[1]);
  });

  it("освобождение без acquire ничего не ломает", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();
    release();
    await Promise.resolve();

    // Счётчик дошёл до нуля до подключения — закрывать нечего.
    expect(adapter.disconnectCalls).toBe(0);

    await connection.start();

    // И явная остановка по-прежнему работает: ею пользуется владелец соединения,
    // а не потребитель.
    await connection.stop();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("подписки позднего потребителя не теряются", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();
    await connection.start({ PlayerStateChange: vi.fn() });

    // Второй потребитель приходит позже и приносит свои обработчики.
    const second = connection.acquire();
    await connection.start({ QueueChanged: vi.fn() });

    adapter.emitEvent("PlayerStateChange", { volume: 1 });
    adapter.emitEvent("QueueChanged", { queue: [] });

    release();
    second();
    await Promise.resolve();

    expect(connection.current()).toBeNull();
  });
});
