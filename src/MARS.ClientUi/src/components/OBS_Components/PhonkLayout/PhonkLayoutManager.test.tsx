import { render, screen, waitFor } from "@testing-library/react";
import { act } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import { setOverlayAdapter } from "@/shared/realtime/overlayHub";

import PhonkLayoutManager from "./PhonkLayoutManager";

/**
 * Компонент оверлея и его подписка на хаб.
 *
 * Проверка нужна потому, что перенос с `useSignalREffect` на `useOverlayEvent`
 * меняет способ подписки, а не только её форму. Прежний мок в тестах был
 * заглушкой `useSignalREffect: () => {}` и ничего не проверял: подписка могла
 * пропасть целиком, и тест оставался зелёным.
 *
 * Здесь связка настоящая: подделка адаптера лежит в реестре, компонент
 * подписывается через хук, а событие вызывает обработчик компонента.
 */
describe("PhonkLayoutManager", () => {
  afterEach(() => {
    setOverlayAdapter(null);
    vi.clearAllMocks();
  });

  it("реагирует на событие PhonkEdit с хаба", async () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    render(<PhonkLayoutManager />);

    // Компонент смонтировался и подписался: emit обязан найти подписчика.
    expect(() => act(() => adapter.emit("PhonkEdit"))).not.toThrow();

    // Алерт по событию появился на экране — значит обработчик сработал.
    await waitFor(() =>
      expect(screen.getByTestId("obs-phonk-layout")).toBeDefined()
    );
  });

  it("отписывается при размонтировании", () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    const { unmount } = render(<PhonkLayoutManager />);
    unmount();

    // Размонтированный экран не должен числиться подписчиком: иначе он
    // продолжал бы получать события и накапливал их в памяти.
    expect(() => adapter.emit("PhonkEdit")).toThrow(/подписчиков нет/);
  });

  it("не падает без подключённого хаба", () => {
    // Стор вызывает start при подъёме приложения, но компонент может
    // смонтироваться раньше. Отсутствие соединения — не ошибка оверлея:
    // фон поверх видео не должен падать из-за канала.
    setOverlayAdapter(null);

    expect(() => render(<PhonkLayoutManager />)).not.toThrow();
  });
});
