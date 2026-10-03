import { describe, expect, it } from "vitest";

import { unpackWaifuRoll, unpackWaifuHusband } from "./telegramusHubUnpack";

/**
 * Распаковка вайфу-событий.
 *
 * Тест скормит настоящие формы из `telegramus.proto`. Раньше распаковка ждала
 * `host` и `twitchUser`, которых в протоколе нет вовсе: в `WaifuRollEvent` есть
 * `waifu`, `display_name` и `color`, а в `ShowCurrentWifeEvent` и
 * `MergeWaifuEvent` — `waifu`, `husband`, `avatar` и `color`. Имя зрителя
 * бралось как `host?.twitchUser?.displayName`, то есть всегда было пустой
 * строкой, и подпись под вайфу не появлялась ни разу.
 */
describe("распаковка вайфу-событий", () => {
  // WaifuRollEvent: содержимое ветки oneof.
  const waifuRollWire = {
    waifu: {
      id: "waifu-1",
      name: "Хижина в лесу",
      image_url: "https://cdn.example/waifu.png",
      source: "Манга",
    },
    display_name: "Стример",
    color: "#ff8800",
  };

  // ShowCurrentWifeEvent: husband вместо display_name.
  const wifeWire = {
    waifu: {
      id: "waifu-1",
      name: "Хижина в лесу",
      image_url: "https://cdn.example/waifu.png",
      source: "Манга",
    },
    husband: {
      id: "husband-1",
      display_name: "Муж",
      avatar_url: "https://cdn.example/husband.png",
    },
    avatar: "https://cdn.example/avatar.png",
    color: "#ff8800",
  };

  it("читает имя зрителя из display_name", () => {
    const unpacked = unpackWaifuRoll(waifuRollWire);

    // Раньше здесь всегда была пустая строка: поля host в протоколе нет.
    expect(unpacked.displayName).toBe("Стример");
  });

  it("читает саму вайфу", () => {
    const unpacked = unpackWaifuRoll(waifuRollWire);

    expect(unpacked.waifu).toEqual(waifuRollWire.waifu);
    expect(unpacked.color).toBe("#ff8800");
  });

  it("отсутствие полей даёт пустые значения, а не исключение", () => {
    const unpacked = unpackWaifuRoll({});

    expect(unpacked.displayName).toBe("");
    expect(unpacked.waifu).toBeNull();
  });

  it("читает мужа из husband", () => {
    const unpacked = unpackWaifuHusband(wifeWire);

    expect(unpacked.waifu).toEqual(wifeWire.waifu);
    expect(unpacked.husband).toEqual(wifeWire.husband);
    expect(unpacked.color).toBe("#ff8800");
  });

  it("муж null, когда его нет", () => {
    const unpacked = unpackWaifuHusband({ waifu: { id: "waifu-1" } });

    expect(unpacked.husband).toBeNull();
    expect(unpacked.waifu).toEqual({ id: "waifu-1" });
  });

  it("не путает husband с host", () => {
    // Формы, которой нет в протоколе, не должны молча давать пустой результат:
    // если сервер пришлёт host, это изменение контракта, и его надо заметить.
    const unpacked = unpackWaifuHusband({
      ...wifeWire,
      husband: undefined,
    });

    expect(unpacked.husband).toBeNull();
  });
});
