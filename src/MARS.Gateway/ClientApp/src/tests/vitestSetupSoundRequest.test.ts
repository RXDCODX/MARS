import { describe, expect, it } from "vitest";

import { SoundRequest } from "@/shared/api";

/**
 * Заглушка сгенерированного клиента SoundRequest.
 *
 * Тест защищает от ловушки, которая уже стоила одного непойманного падения: в
 * заглушке был перечислен единственный метод, и любой тест, смонтировавший пульт
 * или экран трека, упал бы с «not a function». Выглядело бы это как дефект
 * компонента, и чинить пришлось бы не то место.
 */
describe("заглушка клиента очереди звуковых запросов", () => {
  it("отвечает на любой метод клиента", async () => {
    // Методы, которые реально зовёт клиент.
    const methods = [
      "soundRequestStateList",
      "soundRequestQueueList",
      "soundRequestHistoryQueueItemsList",
      "soundRequestQueueReorderCreate",
    ] as const;

    for (const method of methods) {
      const call = (SoundRequest as unknown as Record<string, unknown>)[method];

      expect(typeof call, `метод ${method} отсутствует в заглушке`).toBe(
        "function"
      );
    }
  });

  it("кладёт данные в конверт, который читают компоненты", async () => {
    // Компоненты читают `response.data.data`, поэтому пустой массив должен быть
    // именно там — иначе `Array.isArray(undefined)` дал бы false и список
    // молча не отрисовался бы.
    const response = await (
      SoundRequest as unknown as Record<
        string,
        (arg: unknown) => Promise<unknown>
      >
    ).soundRequestStateList({});

    expect(response).toEqual({
      data: { success: true, result: [], data: [] },
    });
  });

  it("не бросает на неизвестном методе", async () => {
    // Новый метод в сгенерированном клиенте не должен ронять тест: падать
    // должен сам код компонента, а не заглушка.
    const call = (
      SoundRequest as unknown as Record<
        string,
        (arg: unknown) => Promise<unknown>
      >
    ).soundRequestSomethingBrandNew({});

    await expect(call).resolves.toBeDefined();
  });
});
