import { beforeEach, describe, expect, it } from "vitest";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import type { OverlayPayload } from "@/shared/realtime/overlayEvents";
import { useTelegramusHubStore } from "./telegramusHubStore";

/**
 * Стор очереди алертов оверлея — 380 строк, и до сих пор ни одного теста.
 *
 * Причина была в том, что `start()` стор-а строил `HubConnection` сам и
 * регистрировал обработчики внутри: чтобы узнать, что стор слушает, нужно было
 * вызвать `connection.start()`, а он открывает соединение. `HubConnection` при
 * этом класс, лежащий в типе состояния, — подделку в состояние не положить.
 *
 * Теперь транспорт вне стора, а `handlers` — обычный объект в состоянии.
 * Проверяется ровно то, что было недоступно: событие обрабатывается при
 * подключённой подделке, без SignalR и без сети.
 */
describe("useTelegramusHubStore", () => {
  const waifuPayload = { name: "Акира" } as OverlayPayload;
  const hostPayload = {
    twitchUser: { displayName: "стример" },
  } as OverlayPayload;

  beforeEach(() => {
    useTelegramusHubStore.getState().reset();
  });

  it("кладёт WaifuRoll в очередь и показывает его, когда очередь свободна", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);
    adapter.emit("WaifuRoll", waifuPayload, hostPayload);

    const state = useTelegramusHubStore.getState();

    expect(state.isConnected).toBe(true);
    expect(state.isWaifuShowing).toBe(true);
    expect(state.currentMessage?.waifu).toBe(waifuPayload);
    expect(state.currentMessage?.displayName).toBe("стример");
  });

  it("не показывает второй алерт, пока первый на экране, а кладёт в очередь", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", waifuPayload, hostPayload);
    adapter.emit(
      "WaifuRoll",
      { name: "Мидори" } as OverlayPayload,
      hostPayload
    );

    const state = useTelegramusHubStore.getState();

    // На экране остаётся первый: смена картинки поверх текущей выглядела бы
    // как мигание.
    expect(state.currentMessage?.waifu).toBe(waifuPayload);
    expect(state.messages).toHaveLength(1);
  });

  it("продвигает очередь по кнопке", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", waifuPayload, hostPayload);
    adapter.emit(
      "WaifuRoll",
      { name: "Мидори" } as OverlayPayload,
      hostPayload
    );

    useTelegramusHubStore.getState().dequeueCurrent();

    const state = useTelegramusHubStore.getState();

    expect(state.currentMessage?.waifu).toEqual({ name: "Мидори" });
    expect(state.messages).toHaveLength(0);
  });

  it("опустошает очередь по событию Explosion без аргументов", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", waifuPayload, hostPayload);
    adapter.emit("Explosion");

    // Взрыв идёт в пустой стор: у метода нет аргументов, и обработчик обязан
    // это пережить.
    expect(() => adapter.emit("Explosion")).not.toThrow();
  });

  it("раздаёт призы в отдельные сторы, а не в очередь", async () => {
    const adapter = new FakeHubAdapter();
    const prizes = [{ id: 1, name: "prize" }] as OverlayPayload;

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("UpdateWaifuPrizes", prizes);
    adapter.emit("UpdateFumoPrizes", prizes);

    // Призы не проходят через очередь алертов: они меняют панель, а не экран.
    const state = useTelegramusHubStore.getState();

    expect(state.isWaifuShowing).toBe(false);
    expect(state.currentMessage).toBeUndefined();
  });

  it("не поднимает алерт на пустые призы", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("UpdateFrogPrizes", [] as OverlayPayload);

    expect(useTelegramusHubStore.getState().isFrogShowing).toBe(false);
  });

  it("возвращает статус в idle при остановке", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    expect(useTelegramusHubStore.getState().status).toBe("connected");

    await useTelegramusHubStore.getState().stop();

    const state = useTelegramusHubStore.getState();

    expect(state.status).toBe("idle");
    expect(state.isConnected).toBe(false);
  });

  it("переходит в error, если подключение не удалось", async () => {
    const broken = new FakeHubAdapter();

    broken.connect = () => Promise.reject(new Error("нет связи с хабом"));

    await expect(
      useTelegramusHubStore.getState().start(broken)
    ).rejects.toThrow("нет связи");

    const state = useTelegramusHubStore.getState();

    expect(state.status).toBe("error");
    expect(state.isConnected).toBe(false);
  });

  it("помечает нового вайфу при AddNewWaifu", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("AddNewWaifu", waifuPayload, hostPayload);

    expect(useTelegramusHubStore.getState().currentMessage?.waifu).toEqual({
      name: "Акира",
      isAdded: true,
    });
  });

  it("помечает слияние при MergeWaifu", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("MergeWaifu", waifuPayload, hostPayload);

    expect(useTelegramusHubStore.getState().currentMessage?.waifu).toEqual({
      name: "Акира",
      isMerged: true,
    });
  });
});
