import { render } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import { setOverlayAdapter } from "@/shared/realtime/overlayHub";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";

/**
 * Компонент, который только подписывается и ничего не рисует.
 *
 * Отдельный компонент вместо боевого нужен затем, чтобы проверка касалась
 * жизненного цикла подписки, а не разметки.
 */
function Subscriber({ event }: { event: "Credits" }) {
  useOverlayEvent(event, () => undefined);

  return <div data-testid="subscriber" />;
}

/**
 * Размонтированный компонент не должен оставаться подписчиком.
 *
 * Смысл проверки — в том, что утечка между тестами делает результат
 * недостоверным в обе стороны. Здесь тест сначала монтирует компонент и
 * размонтирует его, а следом проверяет, что подписчиков не осталось. Если
 * очистки размонтирования нет, компонент из первого теста продолжает
 * числиться подписчиком во втором и засчитывает себе чужое событие.
 *
 * На практике это выглядит так: тест на отписку падает, хотя отписка в коде
 * есть, — и наоборот, тест на получение события проходит, хотя подписка могла
 * и не сработать, потому что событие ушло компоненту из соседнего теста.
 */
describe("жизненный цикл подписки между тестами", () => {
  afterEach(() => {
    // Очистки здесь намеренно нет: её выполняет настройка тестов, и проверка
    // ниже тем и ценна, что опирается на общий механизм. Собственный cleanup
    // внутри теста скрыл бы поломку настройки — а поломка скрывает дыру в
    // проверках компонентов по всему набору.
    setOverlayAdapter(null);
  });

  it("первый тест: компонент подписывается", () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    render(<Subscriber event="Credits" />);

    expect(() => adapter.emit("Credits")).not.toThrow();
  });

  it("второй тест: подписчиков после размонтирования не осталось", () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    const { unmount } = render(<Subscriber event="Credits" />);
    unmount();

    expect(() => adapter.emit("Credits")).toThrow(/подписчиков нет/);
  });
});
