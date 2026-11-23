import { describe, expect, it } from "vitest";

import { readRewardsList } from "./rewardsResponse";

/**
 * Ответ `/api/twitch/rewards`.
 *
 * Контроллер отдаёт `OperationResult<GetCustomRewardsResponse?>`, а
 * `GetCustomRewardsResponse` — это `{ data?: CustomReward[] }`. Полезная
 * нагрузка конверта поэтому объект, а список лежит ещё на уровень глубже.
 */
const wire = {
  success: true,
  result: {
    data: [{ id: "reward-1", title: "Награда", cost: 100 }],
  },
};

describe("разбор списка наград", () => {
  it("достаёт список из обёртки Helix", () => {
    const read = readRewardsList(wire);

    expect(read.ok).toBe(true);
    expect(read.ok && read.rewards).toHaveLength(1);
  });

  it("форма без обёртки тоже читается", () => {
    // У `{ success, result }` поля `data` нет: транспорт кладёт полезную
    // нагрузку и туда, и в `data`, но полезной нагрузкой может оказаться и
    // голый массив — такой ответ обязан пониматься, а не считаться ошибкой.
    const read = readRewardsList({
      success: true,
      result: [{ id: "reward-1" }],
    });

    expect(read.ok).toBe(true);
    expect(read.ok && read.rewards).toHaveLength(1);
  });

  it("пустая обёртка — это «наград нет», а не «ответ не тот»", () => {
    const read = readRewardsList({ success: true, result: { data: [] } });

    expect(read.ok).toBe(true);
    expect(read.ok && read.rewards).toEqual([]);
  });

  it("объект без списка отличается от пустого списка", () => {
    // Именно это различие прятал дефект: `Array.isArray` на объекте даёт
    // `false`, и пустой список выглядел как «наград нет».
    expect(readRewardsList({ success: true, result: {} }).ok).toBe(false);
  });

  it("отказ отдаёт текст сервиса", () => {
    const read = readRewardsList({ success: false, errorMessage: "Нет прав" });

    expect(read.ok === false && read.message).toBe("Нет прав");
  });

  it("мусор вместо ответа не выдаётся за список", () => {
    expect(readRewardsList("привет").ok).toBe(false);
    expect(readRewardsList(null).ok).toBe(false);
    expect(readRewardsList({ success: true, result: null }).ok).toBe(false);
  });
});
