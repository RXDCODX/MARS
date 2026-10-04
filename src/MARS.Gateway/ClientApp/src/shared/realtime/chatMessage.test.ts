import { describe, expect, it } from "vitest";

import { normalizeChatMessage } from "./chatMessage";

/**
 * Сообщение чата на проводе.
 *
 * Форма снята не из `data-contracts.ts`, а с фактической сериализации
 * `ChatMessageEvent` из `MARS.Shared` теми же настройками, что и у хаба
 * (camelCase). Раньше тесты кормили экраны формой Twurple, то есть проверяли
 * не то, что приезжает: `displayName`, `hexColor` и `userDetail` на проводе
 * нет, и без приведения имя зрителя, цвет и рамки VIP не работали никогда.
 */
const wireMessage = {
  userId: "785975641",
  userName: "RXDCODX",
  message: "привет",
  timestamp: "2026-10-04T00:00:00Z",
  isModerator: true,
  isVip: false,
  isBroadcaster: false,
  chatColor: "#FF0000",
  customRewardId: null,
};

describe("приведение сообщения чата", () => {
  it("читает ник, который на проводе лежит в userName", () => {
    const message = normalizeChatMessage(wireMessage, "msg-1");

    expect(message?.displayName).toBe("RXDCODX");
  });

  it("читает цвет, который на проводе лежит в chatColor", () => {
    const message = normalizeChatMessage(wireMessage, "msg-1");

    expect(message?.hexColor).toBe("#FF0000");
  });

  it("собирает userDetail из полей, которые на проводе лежат наверху", () => {
    const message = normalizeChatMessage(wireMessage, "msg-1");

    // На проводе `isVip` и `isModerator` лежат на верхнем уровне, а экраны
    // читают `userDetail`. Без сборки рамка не появлялась.
    expect(message?.userDetail?.isModerator).toBe(true);
    expect(message?.userDetail?.isVip).toBe(false);
    expect(message?.isBroadcaster).toBe(false);
  });

  it("подставляет белый цвет, когда на проводе его нет", () => {
    const message = normalizeChatMessage(
      { ...wireMessage, chatColor: null },
      "msg-1"
    );

    expect(message?.hexColor).toBe("white");
  });

  it("берёт идентификатор из ветки, а без него — из userId", () => {
    expect(normalizeChatMessage(wireMessage, "msg-1")?.id).toBe("msg-1");
    expect(normalizeChatMessage(wireMessage, undefined)?.id).toBe("785975641");
  });

  it("не показывает сообщение без текста", () => {
    expect(normalizeChatMessage({ userName: "RXDCODX" }, "msg-1")).toBeNull();
    expect(normalizeChatMessage({ message: 42 }, "msg-1")).toBeNull();
  });

  it("не падает на мусоре вместо сообщения", () => {
    expect(normalizeChatMessage(null, "msg-1")).toBeNull();
    expect(normalizeChatMessage("привет", "msg-1")).toBeNull();
    expect(normalizeChatMessage([{ message: "привет" }], "msg-1")).toBeNull();
  });

  it("противоречие провода и контракта видно: полей Twurple на проводе нет", () => {
    // Намеренная проверка формы: если разбор вернётся к proto-именам или к
    // `ChatMessage`, она станет красной, а не тихо перестанет работать в бою.
    expect(wireMessage).not.toHaveProperty("displayName");
    expect(wireMessage).not.toHaveProperty("hexColor");
    expect(wireMessage).not.toHaveProperty("userDetail");
  });
});
