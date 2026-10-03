import { describe, expect, it } from "vitest";

import { readQueueItems } from "./queuePayload";

/**
 * Разбор состояния очереди звуковых запросов.
 *
 * Сервер шлёт в метод хаба содержимое ветки `queue_changed`, то есть сообщение
 * `QueueState`. В proto у него ровно одно поле — `repeated QueueItemSnapshot queue`,
 * — поэтому на проводу это объект `{ queue: [...] }`, а не массив.
 *
 * Клиент ждал массив и клал в стор объект: длина была `undefined`, очередь
 * становилась неопределённым значением, а список на экране не обновлялся.
 * Тест на форму нужен и потому, что сама проверка «пришёл массив» прошла бы на
 * подделке, собранной вручную по неверному предположению.
 */
describe("разбор состояния очереди", () => {
  const items = [
    { id: "a", title: "Первый трек" },
    { id: "b", title: "Второй трек" },
  ];

  it("разворачивает конверт QueueState в список", () => {
    // Что реально едет в метод QueueChanged.
    expect(readQueueItems({ queue: items })).toEqual(items);
  });

  it("принимает пустую очередь", () => {
    expect(readQueueItems({ queue: [] })).toEqual([]);
  });

  it("не путает отсутствие очереди с пустой очередью", () => {
    // Очереди нет — значит неизвестно, а зрителей нет — значит пусто. Разница
    // видна в UI, и подменять одно другим нельзя.
    expect(readQueueItems({})).toBeNull();
    expect(readQueueItems(null)).toBeNull();
    expect(readQueueItems({ queue: null })).toBeNull();
  });

  it("не принимает одиночный элемент за список", () => {
    // `queue` — repeated, но ошибочная сборка могла прислать один объект.
    // Принимать его за список значило бы потерять остальные треки.
    expect(readQueueItems({ queue: { id: "a" } })).toBeNull();
  });
});
