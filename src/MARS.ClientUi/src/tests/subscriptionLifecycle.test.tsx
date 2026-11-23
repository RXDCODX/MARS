import { render } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import {
  overlayEventSubscriberCount,
  resetOverlayEventBus,
} from "@/shared/api/realtime/overlayEventBus";
import { useOverlayEvent } from "@/shared/api/realtime/useOverlayEvent";

/**
 * Компонент, который только подписывается и ничего не рисует.
 */
function Subscriber({ event }: { event: "Credits" }) {
  useOverlayEvent(event, () => undefined);

  return <div data-testid="subscriber" />;
}

describe("жизненный цикл подписки между тестами", () => {
  afterEach(() => {
    resetOverlayEventBus();
  });

  it("первый тест: компонент подписывается на шину", () => {
    render(<Subscriber event="Credits" />);

    expect(overlayEventSubscriberCount("Credits")).toBe(1);
  });

  it("второй тест: подписчиков после размонтирования не осталось", () => {
    const { unmount } = render(<Subscriber event="Credits" />);
    unmount();

    expect(overlayEventSubscriberCount("Credits")).toBe(0);
  });
});