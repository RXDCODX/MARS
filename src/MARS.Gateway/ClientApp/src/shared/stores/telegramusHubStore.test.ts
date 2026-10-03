import { beforeEach, describe, expect, it, vi } from "vitest";

import { getOverlayAdapter } from "@/shared/realtime/overlayHub";
import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import type { OverlayPayload } from "@/shared/realtime/overlayEvents";
import { useTelegramusHubStore } from "./telegramusHubStore";
import useFumoPrizesStore from "./fumoPrizesStore";
import useWaifuPrizesStore from "./waifuPrizesStore";

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
  // Форма WaifuRollEvent из telegramus.proto: waifu, display_name, color.
  // Поля host в протоколе нет вовсе, и раньше фикстура держала выдуманную форму,
  // поэтому тест был зелёным на коде, который на стенде давал пустое имя.
  const displayNamePayload = "стример";

  beforeEach(() => {
    useTelegramusHubStore.getState().reset();
  });

  it("кладёт WaifuRoll в очередь и показывает его, когда очередь свободна", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);
    adapter.emit("WaifuRoll", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });

    const state = useTelegramusHubStore.getState();

    expect(state.isConnected).toBe(true);
    expect(state.isWaifuShowing).toBe(true);
    expect(state.currentMessage?.waifu).toBe(waifuPayload);
    expect(state.currentMessage?.displayName).toBe("стример");
  });

  it("не показывает второй алерт, пока первый на экране, а кладёт в очередь", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });
    adapter.emit("WaifuRoll", {
      waifu: { name: "Мидори" } as OverlayPayload,
      display_name: displayNamePayload,
    });

    const state = useTelegramusHubStore.getState();

    // На экране остаётся первый: смена картинки поверх текущей выглядела бы
    // как мигание.
    expect(state.currentMessage?.waifu).toBe(waifuPayload);
    expect(state.messages).toHaveLength(1);
  });

  it("продвигает очередь по кнопке", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });
    adapter.emit("WaifuRoll", {
      waifu: { name: "Мидори" } as OverlayPayload,
      display_name: displayNamePayload,
    });

    useTelegramusHubStore.getState().dequeueCurrent();

    const state = useTelegramusHubStore.getState();

    expect(state.currentMessage?.waifu).toEqual({ name: "Мидори" });
    expect(state.messages).toHaveLength(0);
  });

  it("опустошает очередь по событию Explosion без аргументов", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("WaifuRoll", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });
    adapter.emit("Explosion");

    // Взрыв идёт в пустой стор: у метода нет аргументов, и обработчик обязан
    // это пережить.
    expect(() => adapter.emit("Explosion")).not.toThrow();
  });

  it("раздаёт призы в отдельные сторы, а не в очередь", async () => {
    const adapter = new FakeHubAdapter();

    // PrizesEvent на проводе: единственное поле prizes_json типа bytes, то есть
    // массив чисел внутри объекта. Раньше тест эмитил готовый массив, и разбор
    // отдавал null: проверялась очередь алертов, а призы не проверялись вовсе.
    const prizes = [{ id: "waifu-1", name: "prize" }];
    const wire = {
      prizesJson: [...new TextEncoder().encode(JSON.stringify(prizes))],
    };

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("UpdateWaifuPrizes", wire);
    adapter.emit("UpdateFumoPrizes", wire);

    // Призы не проходят через очередь алертов: они меняют панель, а не экран.
    const state = useTelegramusHubStore.getState();

    expect(state.isWaifuShowing).toBe(false);
    expect(state.currentMessage).toBeUndefined();

    // И, наконец, утверждаем то, ради чего событие и приходит: призы в сторе.
    // Без этой проверки тест был зелёным при полностью сломанном пути.
    expect(useWaifuPrizesStore.getState().prizes).toEqual(prizes);
    expect(useFumoPrizesStore.getState().prizes).toEqual(prizes);
  });

  it("не поднимает алерт на пустые призы", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("UpdateFrogPrizes", [] as OverlayPayload);

    expect(useTelegramusHubStore.getState().isFrogShowing).toBe(false);
  });

  it("не создаёт второе соединение при повторном start", async () => {
    // Четыре компонента вызывают startHub() в useEffect, а StrictMode вызывает
    // эффекты дважды. Если бы каждый вызов создавал свой адаптер, на странице
    // висело бы несколько соединений, и обработчики стора зарегистрировались
    // бы на каждом — одно событие пришло бы на очередь четыре раза.
    const first = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(first);
    await useTelegramusHubStore.getState().start(first);

    expect(first.status).toBe("connected");
  });

  it("не начинает второе подключение при параллельном start", async () => {
    // Гонка, найденная ревью: `connected` присваивается до await, поэтому на
    // маршруте `/waifu`, где ребенок и родитель попадают в одну пачку пассивных
    // эффектов, два вызова доходили до одного адаптера. Настоящий SignalR второй
    // такой вызов отвергает («Cannot start a HubConnection that is not in the
    // 'Disconnected' state»), а catch обнулял адаптер — и все подписки документа
    // оставались без хаба при зелёном индикаторе.
    let releaseConnect = () => undefined as void;
    const adapter = new FakeHubAdapter();
    let connectCalls = 0;

    adapter.connect = vi.fn(() => {
      connectCalls += 1;

      return new Promise<void>(resolve => {
        releaseConnect = () => resolve();
      });
    }) as unknown as typeof adapter.connect;

    useTelegramusHubStore.getState().reset();

    const first = useTelegramusHubStore.getState().start(adapter);
    const second = useTelegramusHubStore.getState().start(adapter);

    releaseConnect();
    await Promise.all([first, second]);

    // Подключение одно, и обработчики стора зарегистрированы на нём один раз —
    // иначе каждый оверлейный алерт ложился бы в очередь дважды.
    expect(connectCalls).toBe(1);
  });

  it("переиспользует подключённый адаптер вместо создания нового", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    // Второй вызов без аргумента обязан вернуть тот же адаптер. Создать новый
    // он не может: подписки компонентов висят на прежнем.
    await useTelegramusHubStore.getState().start();

    const state = useTelegramusHubStore.getState();

    expect(state.isConnected).toBe(true);
    expect(getOverlayAdapter()).toBe(adapter);
  });

  it("освобождает адаптер при stop, чтобы следующий start создал новый", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);
    await useTelegramusHubStore.getState().stop();

    expect(getOverlayAdapter()).toBeNull();
    expect(adapter.status).toBe("disconnected");
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

    adapter.emit("AddNewWaifu", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });

    expect(useTelegramusHubStore.getState().currentMessage?.waifu).toEqual({
      name: "Акира",
      isAdded: true,
    });
  });

  it("помечает слияние при MergeWaifu", async () => {
    const adapter = new FakeHubAdapter();

    await useTelegramusHubStore.getState().start(adapter);

    adapter.emit("MergeWaifu", {
      waifu: waifuPayload,
      display_name: displayNamePayload,
    });

    expect(useTelegramusHubStore.getState().currentMessage?.waifu).toEqual({
      name: "Акира",
      isMerged: true,
    });
  });
});
