import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { setOverlayAdapter } from "./overlayHub";
import { subscribeToOverlayEvent } from "./overlaySubscription";

/**
 * Подписка стора на событие оверлейного хаба.
 *
 * Стор живёт весь сеанс приложения, а хаб поднимается позже — когда первый
 * компонент вызовет start. Раньше подписка делалась прямо в теле create(...)
 * и читала реестр один раз: адаптера там ещё не было, подписка не
 * создавалась, и событие не приходило никогда. Это выглядело как «хаб
 * подключён, но конфигурация не приезжает».
 */
describe("подписка стора на событие оверлея", () => {
  it("подписывается сразу, если адаптер уже подключён", () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    const handler = vi.fn();
    subscribeToOverlayEvent("AdhdConfig", handler);

    adapter.emitEvent("AdhdConfig", { adhdConfigJson: [] });
    adapter.emitEvent("AdhdConfig", { adhdConfigJson: [] });

    expect(handler).toHaveBeenCalledTimes(2);
  });

  it("подписывается и после того, как адаптер появился", () => {
    // Порядок на стенде: стор импортирован, компонент ещё не монтировался,
    // хаб не поднят. Подписка обязана дождаться адаптера.
    const handler = vi.fn();
    subscribeToOverlayEvent("PostTwitchInfo", handler);

    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    adapter.emitEvent("PostTwitchInfo", { clientId: "id" });

    expect(handler).toHaveBeenCalledWith({ clientId: "id" });
  });

  it("переносит подписку на новый адаптер после переподключения", () => {
    const first = new FakeHubAdapter();
    setOverlayAdapter(first);

    const handler = vi.fn();
    subscribeToOverlayEvent("AdhdConfig", handler);

    first.emitEvent("AdhdConfig", { adhdConfigJson: [] });
    expect(handler).toHaveBeenCalledTimes(1);

    // Обрыв и новое соединение: подписка должна пережить смену адаптера,
    // иначе события заработали бы один раз и замолчали навсегда.
    const second = new FakeHubAdapter();
    setOverlayAdapter(second);
    second.emitEvent("AdhdConfig", { adhdConfigJson: [] });

    expect(handler).toHaveBeenCalledTimes(2);
  });

  it("отписка останавливает доставку", () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    const handler = vi.fn();
    const unsubscribe = subscribeToOverlayEvent("AdhdConfig", handler);

    adapter.emitEvent("AdhdConfig", { adhdConfigJson: [] });
    unsubscribe();
    adapter.emitEvent("AdhdConfig", { adhdConfigJson: [] });

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("отписка до прихода адаптера не оставляет подписки", () => {
    const handler = vi.fn();
    const unsubscribe = subscribeToOverlayEvent("AdhdConfig", handler);

    unsubscribe();

    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);
    adapter.emitEvent("AdhdConfig", { adhdConfigJson: [] });

    expect(handler).not.toHaveBeenCalled();
  });

  it("при смене адаптера старая подписка снимается", () => {
    // Иначе на старом адаптере остаётся обработчик, а сам адаптер ещё и утекает.
    const first = new FakeHubAdapter();
    setOverlayAdapter(first);

    const handler = vi.fn();
    subscribeToOverlayEvent("AdhdConfig", handler);

    const second = new FakeHubAdapter();
    setOverlayAdapter(second);

    expect(() =>
      first.emitEvent("AdhdConfig", { adhdConfigJson: [] })
    ).toThrow();
  });
});
