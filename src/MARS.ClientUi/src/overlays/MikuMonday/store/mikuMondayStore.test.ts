import { afterEach, describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "@/shared/api/realtime/FakeHubAdapter";
import {
  overlayEventSubscriberCount,
  resetOverlayEventBus,
} from "@/shared/api/realtime/overlayEventBus";
import { useOverlayHubStore } from "@/shared/stores/overlayHubStore";

import { useMikuMondayStore } from "./mikuMondayStore";

/**
 * Стор понедельника Miku.
 *
 * Тест закрывает два отказа ревью. Стор строил своё соединение склейкой строки
 * `${import.meta.env.VITE_BASE_PATH}hubs/overlay`: при незаданной переменной это
 * давало `undefinedhubs/overlay`, и адрес отличался от общего оверлейного
 * соединения. А `stop` разрывал адаптер, который не создавал, то есть уводил
 * оверлей у всех остальных компонентов страницы.
 */
describe("стор понедельника Miku", () => {
  afterEach(() => {
    resetOverlayEventBus();
    useOverlayHubStore.getState().reset();
    useMikuMondayStore.getState().reset();
  });

  it("переиспользует уже подключённый оверлейный адаптер", async () => {
    const shared = new FakeHubAdapter();
    await useOverlayHubStore.getState().start(shared);

    await useMikuMondayStore.getState().start();

    // Второе соединение к тому же хабу означало бы, что событие MikuMonday
    // приходит в отдельный сокет, а не в тот, которым слушает оверлей.
    expect(useMikuMondayStore.getState().status).toBe("connected");
    // Общий адаптер уже подключён: повторный connect не нужен и недопустим.
    expect(shared.connectCalls).toBe(1);
  });

  it("ждёт оверлейный адаптер, если он поднимется позже", async () => {
    // Порядок на стенде: React выполняет эффекты детей раньше родительских,
    // поэтому MikuMondayController может смонтироваться раньше, чем владелец
    // оверлейного соединения положит адаптер в реестр. Чтение реестра один раз
    // давало null, и стор поднимал второе соединение к тому же хабу.
    useOverlayHubStore.getState().reset();

    const started = useMikuMondayStore.getState().start();
    const shared = new FakeHubAdapter();
    await useOverlayHubStore.getState().start(shared);

    await started;

    expect(useMikuMondayStore.getState().status).toBe("connected");
    // Своего соединения не строилось: общий стор подключил shared.
    expect(shared.connectCalls).toBe(1);
  });

  it("отказ при запросе треков не глушит алерты", async () => {
    const shared = new FakeHubAdapter();
    // Метода MikuMondayTracks у хаба нет — так и на стенде.
    vi.spyOn(shared, "send").mockRejectedValue(
      new Error("Unknown hub method 'MikuMondayTracks'")
    );
    vi.spyOn(console, "warn").mockImplementation(() => undefined);
    await useOverlayHubStore.getState().start(shared);

    await useMikuMondayStore.getState().start();

    // Раньше отказ уносил с собой подписку на MikuMonday, и на /MikuMonday
    // алерты не приходили никогда — при зелёном индикаторе подключения.
    shared.emitEvent("MikuMonday", {
      id: "alert-9",
      selectedTrack: { id: "track-9", number: 9, title: "Трек" },
      twitchUser: { twitchId: "9", displayName: "Стример" },
    });

    expect(useMikuMondayStore.getState().currentAlert?.id).toBe("alert-9");
  });

  it("получает MikuMonday через общий оверлейный хаб", async () => {
    const shared = new FakeHubAdapter();
    await useOverlayHubStore.getState().start(shared);
    await useMikuMondayStore.getState().start();

    shared.emitEvent("MikuMonday", {
      id: "alert-1",
      selectedTrack: {
        id: "track-1",
        number: 1,
        title: "Трек",
      },
      twitchUser: {
        twitchId: "42",
        displayName: "Стример",
      },
    });

    expect(useMikuMondayStore.getState().currentAlert?.id).toBe("alert-1");
  });

  it("не разрывает чужое соединение при остановке", async () => {
    const shared = new FakeHubAdapter();
    await useOverlayHubStore.getState().start(shared);

    await useMikuMondayStore.getState().start();
    await useMikuMondayStore.getState().stop();

    // MikuMonday не владеет соединением: его остановка не закрывает общий хаб.
    expect(shared.disconnectCalls).toBe(0);
    expect(useMikuMondayStore.getState().status).toBe("idle");
  });

  it("снимает свою подписку при остановке", async () => {
    const shared = new FakeHubAdapter();
    await useOverlayHubStore.getState().start(shared);

    await useMikuMondayStore.getState().start();

    const before = useMikuMondayStore.getState().alerts.length;

    await useMikuMondayStore.getState().stop();

    // После остановки событие не должно попадать в стор: подписка обязана быть
    // снята, а не остаться на адаптере навсегда.
    expect(overlayEventSubscriberCount("MikuMonday")).toBe(0);
    expect(useMikuMondayStore.getState().alerts.length).toBe(before);
  });
});
