import { describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { createHubConnection } from "./hubConnection";

/**
 * Владение соединением между несколькими потребителями.
 *
 * На хаб очереди звуковых запросов один сокет на документ, а потребителей два:
 * пульт и видеоэкран. Раньше размонтирование видеоэкрана вызывало `stop()` и
 * закрывало соединение у пульта, оставляя его с мёртвым адаптером: `SkipTrack` и
 * `FrontStateChange` откатывались с «connection is disconnected», а
 * `PlayerStateChange` переставали приходить — при живом на вид плеере.
 */
describe("владение соединением между потребителями", () => {
  it("соединение живёт, пока жив хотя бы один потребитель", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const releaseFirst = connection.acquire();
    await connection.start();

    const releaseSecond = connection.acquire();

    // Первый потребитель уходит: второй ещё держит соединение.
    releaseFirst();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(0);

    releaseSecond();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("размонтирование в StrictMode не закрывает соединение", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    // Два настоящих потребителя: пульт и видеоэкран.
    const releasePlayer = connection.acquire();
    const releaseScreen = connection.acquire();
    await connection.start();

    // StrictMode отрабатывает эффект и его cleanup второй раз. Если бы каждая
    // такая пара уменьшала счётчик на единицу, соединение закрылось бы, пока
    // оба потребителя ещё на экране.
    const transientPlayer = connection.acquire();
    const transientScreen = connection.acquire();
    transientPlayer();
    transientScreen();

    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(0);

    releasePlayer();
    await Promise.resolve();
    // Один потребитель ещё жив.
    expect(adapter.disconnectCalls).toBe(0);

    releaseScreen();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("повторная отписка не закрывает соединение у живого потребителя", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const releaseFirst = connection.acquire();
    const releaseSecond = connection.acquire();
    await connection.start();

    // Cleanup может прийти дважды. Идемпотентная отписка обязана не съесть
    // чужое владение, иначе соединение закрылось бы под работающим потребителем.
    releaseFirst();
    releaseFirst();
    await Promise.resolve();

    expect(adapter.disconnectCalls).toBe(0);

    releaseSecond();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отписка до старта не оставляет живой канал", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();

    // Порядок в StrictMode бывает обратным: отписка приходит раньше старта.
    // Если бы канал после этого остался жить, потребитель ушёл бы без
    // подписки, но с открытым сокетом на хаб.
    release();
    await connection.start();

    await vi.waitFor(() => {
      expect(adapter.disconnectCalls).toBe(1);
    });
    expect(connection.current()).toBeNull();
  });

  it("start без владельца открывает канал и держит его", async () => {
    // Явный вызов start без acquire — законный сценарий одиночного хаба:
    // закрывать такой канал нельзя, иначе он не поднялся бы никогда.
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    await connection.start();

    expect(connection.current()).toBe(adapter);
    expect(adapter.disconnectCalls).toBe(0);
  });

  it("повторный acquire не закрывает соединение раньше времени", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    await connection.start();

    const releases = [
      connection.acquire(),
      connection.acquire(),
      connection.acquire(),
    ];

    releases.forEach(release => release());
    await Promise.resolve();

    // Три потребителя отпустили — соединение закрылось один раз.
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отсутствие потребителей означает, что старт можно повторить", async () => {
    const adapters: FakeHubAdapter[] = [];
    const connection = createHubConnection(() => {
      const adapter = new FakeHubAdapter();
      adapters.push(adapter);

      return adapter;
    });

    const release = connection.acquire();
    await connection.start();
    release();
    await Promise.resolve();

    await connection.start();

    expect(adapters).toHaveLength(2);
    expect(connection.current()).toBe(adapters[1]);
  });

  it("освобождение без acquire ничего не ломает", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();
    release();
    await Promise.resolve();

    // Счётчик дошёл до нуля до подключения — закрывать нечего.
    expect(adapter.disconnectCalls).toBe(0);

    await connection.start();

    // И явная остановка по-прежнему работает: ею пользуется владелец соединения,
    // а не потребитель.
    await connection.stop();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отпускание владения снимает подписки этого потребителя", async () => {
    // Регистрация через `on` возвращает отписку, и она не должна выбрасываться.
    // Раньше она терялась, и каждый вход на `/player`, `/video-screen` и
    // `/scoreboard` добавлял ещё один обработчик на уже подключённый адаптер:
    // событие начинало обрабатываться N раз, а список замыканий размонтированных
    // хуков рос без ограничений.
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    // Сначала канал открывает первый потребитель, потом приходят остальные со
    // своими обработчиками — так и устроены пульт, видеоэкран и табло.
    const owner = connection.acquire();
    await connection.start();
    const second = connection.acquire({ ReceiveState: vi.fn() });
    const third = connection.acquire({ ReceiveState: vi.fn() });

    expect(adapter.subscriberCount("ReceiveState")).toBe(2);

    // Второй потребитель уходит вместе со своей подпиской.
    second();
    await Promise.resolve();

    expect(adapter.subscriberCount("ReceiveState")).toBe(1);
    expect(connection.current()).toBe(adapter);

    // Канал жив, потому что первый потребитель ещё держит соединение.
    owner();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(0);

    third();
    await Promise.resolve();
    expect(adapter.disconnectCalls).toBe(1);
  });

  it("отпускание во время рукопожатия закрывает соединение", async () => {
    // Гонка: уход со страницы в первые миллисекунды подключения. Пока шёл
    // negotiate, connected был null, поэтому прежняя проверка «закрывать, если
    // владельцев не осталось и соединение есть» молча ничего не делала: канал
    // доезжал и жил без владельцев, а следующий потребитель получал адаптер с
    // чужими обработчиками на сокете.
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const release = connection.acquire();
    const opening = connection.start();
    release();

    await opening;
    await vi.waitFor(() => {
      expect(adapter.disconnectCalls).toBe(1);
    });
    expect(connection.current()).toBeNull();
  });

  it("отпускание не снимает чужую запись", async () => {
    // Ветка acquire без обработчиков ничего не кладёт в unattached, поэтому
    // indexOf возвращал -1, а splice(-1, 1) удалял последний элемент чужой
    // записи. Сегодня безвредно только потому, что attachPending массив не
    // чистит, — то есть это счастливое совпадение, а не свойство.
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    const withHandlers = connection.acquire({ ReceiveState: vi.fn() });
    const owner = connection.acquire();
    await connection.start();

    expect(adapter.subscriberCount("ReceiveState")).toBe(1);

    owner();

    // Подписка первого потребителя на месте: его отписка не должна была
    // исчезнуть вместе с записью чужой.
    expect(adapter.subscriberCount("ReceiveState")).toBe(1);

    withHandlers();
  });

  it("подписки позднего потребителя не теряются", async () => {
    const adapter = new FakeHubAdapter();
    const connection = createHubConnection(() => adapter);

    // Первый потребитель открывает канал и отдаёт свою карту в start.
    const release = connection.acquire();
    await connection.start({ PlayerStateChange: vi.fn() });

    // Второй приходит позже и цепляет свои обработчики через on.
    const second = connection.acquire({ QueueChanged: vi.fn() });
    await connection.start();

    adapter.emitEvent("PlayerStateChange", { volume: 1 });
    adapter.emitEvent("QueueChanged", { queue: [] });

    // Оба события обработаны: первый подписчик не заменён, второй не потерян.
    expect(adapter.delivered("PlayerStateChange")).toBe(1);
    expect(adapter.delivered("QueueChanged")).toBe(1);

    release();
    second();
    await Promise.resolve();

    expect(connection.current()).toBeNull();
  });
});
