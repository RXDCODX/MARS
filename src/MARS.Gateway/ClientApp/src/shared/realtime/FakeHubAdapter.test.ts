import { describe, expect, it } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { OVERLAY_EVENT_NAMES } from "./overlayEvents";
import type { OverlayHandlers } from "./overlayEvents";

/**
 * Обработчики, которые можно вызвать вообще без подключения.
 *
 * Раньше обработчики регистрировались внутри `start()` стор-а, а до старта надо
 * было построить `HubConnection` — класс из `@microsoft/signalr`, который в
 * jsdom не существует. Из-за этого у `telegramusHubStore` (380 строк, самый
 * важный стор приложения) не было ни одного теста: логику очереди алертов
 * нельзя было проверить, не подняв реальное соединение.
 *
 * Теперь `handlers` — обычный объект в состоянии стора, а транспорт спрятан за
 * `HubAdapter`. Проверяется ровно то свойство, ради которого всё затевалось:
 * событие обрабатывается, когда адаптер — подделка, а соединения нет.
 */
describe("FakeHubAdapter", () => {
  const creditPayload = { mediaUrl: "/memory/alerts/1.mp4" } as never;

  /**
   * Карта из интересуемого обработчика плюс заглушки на остальные события.
   *
   * Приводить частичный объект к `OverlayHandlers` прямой проверкой нельзя:
   * требование всех 36 обработчиков проверялось бы только на глаз. Достаточно
   * развернуть пустой объект и наложить нужное — тип остаётся точным, а
   * недостающие события тесту не мешают.
   */
  function handlersWith(given: Partial<OverlayHandlers>): OverlayHandlers {
    const missing = {} as OverlayHandlers;

    return { ...missing, ...given };
  }

  it("вызывает обработчик события, когда транспорт — подделка", () => {
    const received: unknown[] = [];
    const adapter = new FakeHubAdapter();

    void adapter.connect(
      handlersWith({ Credits: () => received.push("credits") })
    );
    adapter.emit("Credits");

    expect(received).toEqual(["credits"]);
  });

  it("передаёт полезную нагрузку обработчику", () => {
    const received: unknown[] = [];
    const adapter = new FakeHubAdapter();
    adapter.connect(handlersWith({ Alert: payload => received.push(payload) }));

    adapter.emit("Alert", creditPayload);

    expect(received).toEqual([creditPayload]);
  });

  it("требует подписчика: иначе событие ушло бы в никуда молча", () => {
    const adapter = new FakeHubAdapter();

    expect(() => adapter.emit("Credits")).toThrow(/connect/i);
  });

  it("записывает вызовы клиента для проверки unary-методов", async () => {
    const adapter = new FakeHubAdapter();

    await adapter.invoke("ObsFreeze");
    await adapter.invoke("TwitchMsg", "привет");

    expect(adapter.sent).toEqual([
      { method: "ObsFreeze", args: [] },
      { method: "TwitchMsg", args: ["привет"] },
    ]);
  });

  it("не требует соединения для клиентского вызова", async () => {
    const adapter = new FakeHubAdapter();

    // Вызовы клиент→сервер в оверле не ждут ответа: их результат никто не
    // читает, а в тесте ждать ответа неоткуда. Подделка фиксирует вызов и
    // возвращает управление.
    await expect(adapter.invoke("ObsFreeze")).resolves.toBeUndefined();
    expect(adapter.sent).toHaveLength(1);
    expect(adapter.status).toBe("disconnected");
  });

  it("покрывает все 36 имён событий хаба", () => {
    // Имена приходят из манифеста на стороне C#, и тест на C# сверяет его с
    // интерфейсом хаба. Здесь проверяется, что карта типов и манифест не
    // разошлись: emit() с именем вне манифеста невозможен по типам, а лишний
    // ключ в манифесте поймал бы vitest-тест манифеста.
    expect(OVERLAY_EVENT_NAMES).toHaveLength(36);
    expect(new Set(OVERLAY_EVENT_NAMES).size).toBe(OVERLAY_EVENT_NAMES.length);
  });

  it("не принимает имя события, которого нет в контракте", () => {
    const adapter = new FakeHubAdapter();

    adapter.on("Credits", () => undefined);

    // @ts-expect-error — имена вне карты обязаны быть ошибкой компиляции.
    // Раньше проверки не было вовсе, и useOverlayEvent("deletemessage")
    // расходился с сервером молча.
    expect(() => adapter.emit("credits")).toThrow();
  });
});
