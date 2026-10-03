import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { createHubConnection, type HubConnection } from "./hubConnection";
import { TunaMusicEvent, useTunaEvent } from "./useTunaEvent";

/**
 * Подписка на хаб информации о треке.
 *
 * Тест закрывает реальный отказ: `tunaConnection.start()` не звал никто, а
 * адаптер клался в реестр только внутри `start`. В итоге `getTunaAdapter()`
 * всегда возвращал `null`, `useTunaEvent` молча выходил, и события трека не
 * доходили ни до одного компонента — при подключённом оверлее и «зелёном» стенде.
 */
describe("подписка на хаб информации о треке", () => {
  const music = {
    data: { title: "Kasane Teto", artist: "siIvaGunner" },
    hostname: "tuna",
    timestamp: "2026-10-03T00:00:00Z",
  };

  it("подключается к хабу и получает событие", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);
    const handler = vi.fn<(music: TunaMusicEvent) => void>();

    renderHook(() => useTunaEvent(handler, connection));

    // Подписка должна сама открыть канал: раньше это делал только оверлейный
    // стор, а у хаба тунца своего не было, и подписка висела в воздухе.
    // Ожидание по факту подписки, а не по счётчику подключений: обработчик
    // цепляется в микротаске после `connect`, и проверка одного счётчика была
    // гонкой — событие уходило в пустоту.
    await vi.waitFor(() => {
      adapter.emitEvent("TunaMusicInfo", music);

      expect(handler).toHaveBeenCalledWith(music);
    });

    expect(adapter.connectCalls).toBe(1);
  });

  it("не открывает второе соединение при повторном рендере", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const { rerender } = renderHook(() =>
      useTunaEvent(() => undefined, connection)
    );
    rerender();
    rerender();

    await vi.waitFor(() => {
      expect(adapter.connectCalls).toBe(1);
    });
  });

  it("разбирает ветку события трека", () => {
    // Что приходит в метод: содержимое ветки с полями info-сообщения.
    const received: TunaMusicEvent[] = [];
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    renderHook(
      () => useTunaEvent(value => received.push(value), connection),
      undefined
    );

    return vi.waitFor(() => {
      expect(adapter.connectCalls).toBe(1);

      adapter.emitEvent("TunaMusicInfo", music);
      expect(received).toEqual([music]);
    });
  });

  it("пропускает событие без полезной нагрузки", async () => {
    const adapter = new FakeHubAdapter();
    const connection: HubConnection = createHubConnection(() => adapter);
    const handler = vi.fn();

    renderHook(() => useTunaEvent(handler, connection));

    // Ожидание по факту подписки — до этого момента emitEvent бросает, а
    // проверять «событие без данных» на ещё не подписанном адаптере бессмысленно.
    await vi.waitFor(() => {
      adapter.emitEvent("TunaMusicInfo", { hostname: "tuna" });

      expect(handler).not.toHaveBeenCalled();
    });
  });
});
