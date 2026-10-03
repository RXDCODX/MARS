import { afterEach, describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import { setOverlayAdapter } from "@/shared/realtime/overlayHub";

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
    setOverlayAdapter(null);
    useMikuMondayStore.getState().reset();
  });

  it("переиспользует уже подключённый оверлейный адаптер", async () => {
    const shared = new FakeHubAdapter();
    setOverlayAdapter(shared);

    await useMikuMondayStore.getState().start();

    // Второе соединение к тому же хабу означало бы, что событие MikuMonday
    // приходит в отдельный сокет, а не в тот, которым слушает оверлей.
    expect(useMikuMondayStore.getState().adapter).toBe(shared);
    // Общий адаптер уже подключён: повторный connect не нужен и недопустим.
    expect(shared.connectCalls).toBe(0);
  });

  it("ждёт оверлейный адаптер, если он поднимется позже", async () => {
    // Порядок на стенде: React выполняет эффекты детей раньше родительских,
    // поэтому MikuMondayController может смонтироваться раньше, чем владелец
    // оверлейного соединения положит адаптер в реестр. Чтение реестра один раз
    // давало null, и стор поднимал второе соединение к тому же хабу.
    setOverlayAdapter(null);

    const started = useMikuMondayStore.getState().start();
    const shared = new FakeHubAdapter();
    setOverlayAdapter(shared);

    await started;

    expect(useMikuMondayStore.getState().adapter).toBe(shared);
    // Своего соединения не строилось: shared.connectCalls === 0, отдельного
    // адаптера в состоянии нет.
    expect(shared.connectCalls).toBe(0);
  });

  it("отказ при запросе треков не глушит алерты", async () => {
    const shared = new FakeHubAdapter();
    // Метода MikuMondayTracks у хаба нет — так и на стенде.
    vi.spyOn(shared, "send").mockRejectedValue(
      new Error("Unknown hub method 'MikuMondayTracks'")
    );
    vi.spyOn(console, "warn").mockImplementation(() => undefined);
    setOverlayAdapter(shared);

    await useMikuMondayStore.getState().start();

    // Раньше отказ уносил с собой подписку на MikuMonday, и на /MikuMonday
    // алерты не приходили никогда — при зелёном индикаторе подключения.
    shared.emitEvent("MikuMonday", {
      mikuMondayJson: [
        ...new TextEncoder().encode(
          JSON.stringify({
            id: "alert-9",
            selectedTrack: { id: "track-9", number: 9, title: "Трек" },
            twitchUser: { twitchId: "9", displayName: "Стример" },
          })
        ),
      ],
    });

    expect(useMikuMondayStore.getState().currentAlert?.id).toBe("alert-9");
  });

  it("переносит подписку на новый адаптер после переподключения", async () => {
    const first = new FakeHubAdapter();
    setOverlayAdapter(first);

    await useMikuMondayStore.getState().start();

    const second = new FakeHubAdapter();
    setOverlayAdapter(second);

    // Старая подписка снята, новая на месте: иначе после stop/start внутри SPA
    // события MikuMonday замолчали бы навсегда.
    expect(() =>
      first.emitEvent("MikuMonday", { mikuMondayJson: [] })
    ).toThrow();

    // Форма MikuMondayDto обязана быть полной: стор читает selectedTrack.id,
    // selectedTrack.number и twitchUser.twitchId — без них он упал бы с
    // «displayName of undefined», а не промолчал.
    second.emitEvent("MikuMonday", {
      mikuMondayJson: [
        ...new TextEncoder().encode(
          JSON.stringify({
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
          })
        ),
      ],
    });

    // Очередь была пуста, поэтому алерт становится текущим, а не попадает в
    // очередь. Проверяется факт доставки, а не конкретное поле состояния.
    expect(useMikuMondayStore.getState().currentAlert?.id).toBe("alert-1");
  });

  it("не разрывает чужое соединение при остановке", async () => {
    const shared = new FakeHubAdapter();
    setOverlayAdapter(shared);

    await useMikuMondayStore.getState().start();
    await useMikuMondayStore.getState().stop();

    // Оверлейные компоненты работают на том же адаптере: разрыв здесь уводил бы
    // их все разом, и на стенде это выглядело бы как «сломался оверлей».
    expect(shared.disconnectCalls).toBe(0);
    expect(useMikuMondayStore.getState().adapter).toBeUndefined();
  });

  it("снимает свою подписку при остановке", async () => {
    const shared = new FakeHubAdapter();
    setOverlayAdapter(shared);

    await useMikuMondayStore.getState().start();

    const before = useMikuMondayStore.getState().alerts.length;

    await useMikuMondayStore.getState().stop();

    // После остановки событие не должно попадать в стор: подписка обязана быть
    // снята, а не остаться на адаптере навсегда.
    expect(() => {
      shared.emitEvent("MikuMonday", {
        mikuMondayJson: [
          ...new TextEncoder().encode(
            JSON.stringify({ title: "Поздний трек" })
          ),
        ],
      });
    }).toThrow();
    expect(useMikuMondayStore.getState().alerts.length).toBe(before);
  });
});
