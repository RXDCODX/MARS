import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import {
  buildOverlayBusHandlers,
  overlayEventSubscriberCount,
  resetOverlayEventBus,
} from "./overlayEventBus";
import { useOverlayEvent } from "./useOverlayEvent";

/**
 * Подписка компонента на событие хаба.
 *
 * Заменяет `useSignalREffect` из react-signalr. Разница в проверке имени: раньше
 * хук принимал строку и ничего не проверял, поэтому в проекте живут
 * `useOverlayEvent("DeleteMessage")` и `useOverlayEvent("Adhd")` — такие имена
 * разошлись с сервером и компенсировались тем, что резолвер SignalR
 * регистронезависим. Теперь имя берётся из карты типов, и `deletemessage` не
 * собирается.
 */
describe("useOverlayEvent", () => {
  it("передаёт событие обработчику компонента", () => {
    const adapter = new FakeHubAdapter();
    adapter.connect(buildOverlayBusHandlers());
    const received: unknown[] = [];

    renderHook(() =>
      useOverlayEvent("DeleteMessage", (...args) => received.push(args))
    );

    adapter.emit("DeleteMessage", { id: "1" });

    expect(received).toEqual([[{ id: "1" }]]);
  });

  it("отписывается при размонтировании", () => {
    const adapter = new FakeHubAdapter();
    adapter.connect(buildOverlayBusHandlers());
    const received: unknown[] = [];

    const { unmount } = renderHook(() =>
      useOverlayEvent("DeleteMessage", (...args) => received.push(args))
    );

    unmount();

    // После размонтирования слушатель снят со шины: событие доходит до
    // диспетчера, но обработчик компонента не вызывается.
    adapter.emit("DeleteMessage", { id: "2" });
    expect(received).toEqual([]);
  });

  it("подписывается до connect и получает событие после него", () => {
    // Порядок важен: компонент монтируется раньше, чем кто-то откроет канал.
    // Подписка на шину не требует соединения, поэтому событие, выданное после
    // подключения адаптера, всё равно доходит.
    const adapter = new FakeHubAdapter();
    const received: unknown[] = [];

    renderHook(() =>
      useOverlayEvent("Credits", () => received.push("credits"))
    );

    adapter.connect(buildOverlayBusHandlers());

    expect(() => adapter.emit("Credits")).not.toThrow();

    expect(received).toEqual(["credits"]);
  });

  it("переподписывается при смене обработчика", () => {
    const adapter = new FakeHubAdapter();
    adapter.connect(buildOverlayBusHandlers());
    const received: string[] = [];

    const { rerender } = renderHook(
      ({ tag }: { tag: string }) =>
        useOverlayEvent("Credits", () => received.push(tag)),
      { initialProps: { tag: "первый" } }
    );

    rerender({ tag: "второй" });
    adapter.emit("Credits");

    // Старый обработчик снят, иначе один экран получил бы по два события.
    expect(received).toEqual(["второй"]);
  });

  it("отвергает имя события вне контракта", () => {
    // @ts-expect-error — «deletemessage» нет в карте: имя вне контракта
    // обязано быть ошибкой компиляции. Именно такой подпиской был заполнен
    // клиент монолита — имена писались строкой и разошлись с сервером, а
    // резолвер SignalR был регистронезависим, поэтому расхождение не
    // проявлялось.
    renderHook(() => useOverlayEvent("deletemessage", () => undefined));

    // Подписка под несуществующим именем не регистрируется под реальным
    // событием: шина хранит слушателей по ключу имени, и «deletemessage»
    // не совпадает с «DeleteMessage».
    expect(overlayEventSubscriberCount("DeleteMessage")).toBe(0);
  });

  it("подписка не зависит от адаптера: событие доходит и без реестра", () => {
    // Раньше эта гонка была реальной: компонент, смонтированный раньше
    // соединения, навсегда оставался без подписки. Шина убирает зависимость
    // от адаптера, поэтому подписка работает независимо от того, подключён ли
    // адаптер в момент монтирования.
    const received: string[] = [];

    renderHook(() =>
      useOverlayEvent("Credits", () => received.push("credits"))
    );

    const adapter = new FakeHubAdapter();
    adapter.connect(buildOverlayBusHandlers());

    adapter.emit("Credits");

    expect(received).toEqual(["credits"]);

    resetOverlayEventBus();
  });

  it("отписывается при смене адаптера не требуется: шина одна на документ", () => {
    // Шина не привязана к адаптеру, поэтому смена адаптера не ломает подписку:
    // второй адаптер, подключённый к той же карте, доставляет событие тому же
    // слушателю.
    const first = new FakeHubAdapter();
    const second = new FakeHubAdapter();
    const received: string[] = [];

    first.connect(buildOverlayBusHandlers());
    second.connect(buildOverlayBusHandlers());

    renderHook(() =>
      useOverlayEvent("Credits", () => received.push("событие"))
    );

    second.emit("Credits");
    expect(received).toEqual(["событие"]);

    resetOverlayEventBus();
  });
});