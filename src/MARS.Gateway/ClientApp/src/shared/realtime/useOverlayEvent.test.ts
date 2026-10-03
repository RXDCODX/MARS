import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { useOverlayEvent } from "./useOverlayEvent";

/**
 * Подписка компонента на событие хаба.
 *
 * Заменяет `useSignalREffect` из react-signalr. Разница в проверке имени: раньше
 * хук принимал строку и ничего не проверял, поэтому в проекте живут
 * `useOverlayEvent("deletemessage")` и `useOverlayEvent("adhd")` — такие имена
 * разошлись с сервером и компенсировались тем, что резолвер SignalR
 * регистронезависим. Теперь имя берётся из карты типов, и `deletemessage` не
 * собирается.
 */
describe("useOverlayEvent", () => {
  it("передаёт событие обработчику компонента", () => {
    const adapter = new FakeHubAdapter();
    const received: unknown[] = [];

    renderHook(() =>
      useOverlayEvent(
        "DeleteMessage",
        (...args) => received.push(args),
        adapter
      )
    );

    adapter.emit("DeleteMessage", { id: "1" });

    expect(received).toEqual([[{ id: "1" }]]);
  });

  it("отписывается при размонтировании", () => {
    const adapter = new FakeHubAdapter();
    const received: unknown[] = [];

    const { unmount } = renderHook(() =>
      useOverlayEvent("DeleteMessage", (...args) => received.push(args))
    );

    unmount();

    // После размонтирования подписчиков не остаётся, и emit об этом сообщает.
    // Раньше здесь был тихий no-op: размонтированный экран продолжал числиться
    // подписчиком, а тест на это не смотрел.
    expect(() => adapter.emit("DeleteMessage", { id: "2" })).toThrow(
      /подписчиков нет/
    );
    expect(received).toEqual([]);
  });

  it("подписывается до connect и получает событие после него", () => {
    // Порядок важен: компонент монтируется раньше, чем кто-то откроет канал.
    // Если бы on() требовал соединения, первый же алерт после старта потерялся бы.
    const adapter = new FakeHubAdapter();
    const received: unknown[] = [];

    renderHook(() =>
      useOverlayEvent("Credits", () => received.push("credits"), adapter)
    );

    expect(() => adapter.emit("Credits")).not.toThrow();

    expect(received).toEqual(["credits"]);
  });

  it("переподписывается при смене обработчика", () => {
    const adapter = new FakeHubAdapter();
    const received: string[] = [];

    const { rerender } = renderHook(
      ({ tag }: { tag: string }) =>
        useOverlayEvent("Credits", () => received.push(tag), adapter),
      { initialProps: { tag: "первый" } }
    );

    rerender({ tag: "второй" });
    adapter.emit("Credits");

    // Старый обработчик снят, иначе один экран получил бы по два события.
    expect(received).toEqual(["второй"]);
  });

  it("отвергает имя события вне контракта", () => {
    const adapter = new FakeHubAdapter();

    const { unmount } = renderHook(() =>
      // @ts-expect-error — «deletemessage» нет в карте: имя вне контракта
      // обязано быть ошибкой компиляции. Именно такой подпиской был заполнен
      // клиент монолита — имена писались строкой и разошлись с сервером, а
      // резолвер SignalR был регистронезависим, поэтому расхождение не
      // проявлялось.
      useOverlayEvent("deletemessage", () => undefined, adapter)
    );

    // В рантайме имя не проверяется: защита только на этапе сборки, поэтому
    // подписка под таким именем просто никогда не сработает.
    expect(() => adapter.emit("DeleteMessage", undefined)).toThrow(
      /подписчиков нет/
    );

    unmount();
  });
});
