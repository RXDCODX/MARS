import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { createHubConnection } from "./hubConnection";

/**
 * Жизненный цикл соединения с хабом.
 *
 * Проверяется то, что нельзя увидеть в типах: идемпотентность старта, сохранение
 * обработчиков поздних подписчиков и корректность остановки. Каждый пункт
 * соответствует реальному отказу, найденному ревью.
 */
describe("жизненный цикл соединения с хабом", () => {
  it("не строит второй адаптер при повторном старте", async () => {
    const adapters: FakeHubAdapter[] = [];
    const connection = createHubConnection(() => {
      const adapter = new FakeHubAdapter();
      adapters.push(adapter);

      return adapter;
    });

    const first = await connection.start();
    const second = await connection.start();

    // StrictMode вызывает эффекты дважды: без идемпотентности компонент,
    // смонтированный одновременно с другим, открыл бы второй сокет на тот же хаб.
    expect(second).toBe(first);
    expect(adapters).toHaveLength(1);
  });

  it("одновременные старты ждут одно соединение", async () => {
    const adapters: FakeHubAdapter[] = [];
    const connection = createHubConnection(() => {
      const adapter = new FakeHubAdapter();
      adapters.push(adapter);

      return adapter;
    });

    // Два компонента смонтировались в один кадр — без общего обещания на каждое
    // пришлось бы открывать своё.
    const [first, second] = await Promise.all([
      connection.start(),
      connection.start(),
    ]);

    expect(first).toBe(second);
    expect(adapters).toHaveLength(1);
  });

  it("сохраняет обработчики подписчика, пришедшего после старта", async () => {
    const first = new FakeHubAdapter();
    const second = new FakeHubAdapter();
    const adapters = [first, second];
    let created = 0;

    const connection = createHubConnection(() => {
      const adapter = adapters[created];
      created += 1;

      return adapter;
    });

    const firstHandler = vi.fn();
    await connection.start({ ReceiveState: firstHandler });

    // Первая подписка закрыла соединение, вторая пришла позже. Раньше её
    // обработчики молча выбрасывались, и компонент, подключившийся вторым,
    // не получал ни одного события — при полностью «зелёном» подключении.
    const lateHandler = vi.fn();
    await connection.start({ PlayerStateChange: lateHandler });

    const adapter = connection.current();
    expect(adapter).not.toBeNull();
    expect(adapter).toBe(first);

    first.emitEvent("ReceiveState", { score: 1 });
    first.emitEvent("PlayerStateChange", { name: "RX" });

    expect(firstHandler).toHaveBeenCalledWith({ score: 1 });
    expect(lateHandler).toHaveBeenCalledWith({ name: "RX" });
  });

  it("не оставляет адаптер подключённым после остановки", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    await connection.start();
    expect(connection.current()).not.toBeNull();

    await connection.stop();

    expect(connection.current()).toBeNull();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("повторная остановка безопасна", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    await connection.start();
    await connection.stop();
    // Размонтирование в StrictMode вызывает эффекты дважды: вторая остановка
    // не должна бросать на уже остановленном соединении.
    await expect(connection.stop()).resolves.toBeUndefined();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("после остановки старт строит новое соединение", async () => {
    const adapters: FakeHubAdapter[] = [];
    const connection = createHubConnection(() => {
      const adapter = new FakeHubAdapter();
      adapters.push(adapter);

      return adapter;
    });

    await connection.start();
    await connection.stop();
    await connection.start();

    expect(adapters).toHaveLength(2);
    expect(connection.current()).toBe(adapters[1]);
  });
});
