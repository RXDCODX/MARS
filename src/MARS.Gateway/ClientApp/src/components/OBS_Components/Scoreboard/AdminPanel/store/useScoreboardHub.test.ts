import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import { createHubConnection } from "@/shared/realtime/hubConnection";
import { scoreboardConnection } from "@/shared/realtime/hubConnections";

import { useScoreboardHub } from "./useScoreboardHub";
import { useScoreboardStore } from "./scoreboardStore";

/**
 * Подключение табло к своему хабу.
 *
 * Тест закрывает реальный отказ: соединение поднималось прямо в теле
 * `create(...)`, то есть на импорте модуля. Из-за этого любой тест, задевающий
 * стор, запускал настоящее согласование SignalR и упирался в сетевой таймаут —
 * три «красных» теста падали по таймауту в 5 секунд вместо проверки своего, а
 * в браузере страница без табло всё равно открывала сокет к хабу табло.
 */
describe("подключение табло к хабу", () => {
  it("на импорте стора соединение не поднимается", () => {
    // Модуль уже загружен тестом. Если бы стор открывал канал при импорте,
    // здесь был бы непустой адаптер и начались бы сетевые согласования.
    expect(scoreboardConnection.current()).toBeNull();
  });

  it("хук поднимает соединение и отдаёт состояние в стор", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    renderHook(() => useScoreboardHub(connection));

    await vi.waitFor(() => {
      expect(adapter.connectCalls).toBe(1);
    });

    adapter.emitEvent("ReceiveState", {
      player1: { name: "Локальный", score: 7 },
      isVisible: true,
    });

    // Сервер прислал состояние — стор обязан его принять, иначе табло осталось
    // бы с демонстрационными значениями.
    await vi.waitFor(() => {
      expect(useScoreboardStore.getState().player1.name).toBe("Локальный");
      expect(useScoreboardStore.getState().player1.score).toBe(7);
      expect(useScoreboardStore.getState().hasReceivedInitialState).toBe(true);
    });
  });

  it("не поднимает второе соединение при повторном рендере", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const { rerender } = renderHook(() => useScoreboardHub(connection));
    rerender();
    rerender();

    await vi.waitFor(() => {
      expect(adapter.connectCalls).toBe(1);
    });
  });

  it("отправляет накопленное в очереди после подключения", async () => {
    // Стор — синглтон на весь файл, и предыдущие тесты оставили в нём своё
    // соединение. Без сброса команда ушла бы в чужую подделку, а не в очередь.
    useScoreboardStore.getState()._setConnection(null);

    const store = useScoreboardStore.getState();

    // До подключения команда не уходит: сервера нет, терять её нельзя.
    const queued = await store._sendToServer("SetVisibility", false);
    expect(queued).toBe(false);

    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    renderHook(() => useScoreboardHub(connection));

    await vi.waitFor(() => {
      expect(adapter.sent).toEqual([
        { method: "SetVisibility", args: [false] },
      ]);
    });
  });
});
