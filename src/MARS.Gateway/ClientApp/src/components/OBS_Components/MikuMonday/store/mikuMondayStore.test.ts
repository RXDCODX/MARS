import { afterEach, describe, expect, it } from "vitest";

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
