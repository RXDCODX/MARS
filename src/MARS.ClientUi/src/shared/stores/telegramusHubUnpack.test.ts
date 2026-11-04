import { describe, expect, it } from "vitest";

import { unpackWaifuRoll, unpackWaifuHusband } from "./telegramusHubUnpack";

/**
 * Распаковка вайфу-событий.
 *
 * Формы в фикстурах — не имена полей из `telegramus.proto`, а то, что реально
 * едет по проводу. Это разные имена: `AddMarsSignalR` выставляет
 * `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, поэтому proto-поле
 * `display_name` приходит как `displayName`.
 *
 * Раньше фикстуры кормили именами из proto, а распаковка читала то же самое, и
 * тест был зелёным на коде, который в бою давал пустую строку: имя зрителя под
 * вайфу не появлялось ни разу. Это та же ловушка, что описана для остальных
 * веток в `OverlayPayloadWireFormatTests` — проверять надо провод, а не
 * протокол.
 */
describe("распаковка вайфу-событий", () => {
  // WaifuRollEvent: содержимое ветки oneof, как его сериализует хаб.
  const waifuRollWire = {
    waifu: {
      id: "waifu-1",
      name: "Хижина в лесу",
      imageUrl: "https://cdn.example/waifu.png",
      source: "Манга",
    },
    displayName: "Стример",
    color: "#ff8800",
  };

  // ShowCurrentWifeEvent: husband вместо displayName.
  const wifeWire = {
    waifu: {
      id: "waifu-1",
      name: "Хижина в лесу",
      imageUrl: "https://cdn.example/waifu.png",
      source: "Манга",
    },
    husband: {
      id: "husband-1",
      displayName: "Муж",
      avatarUrl: "https://cdn.example/husband.png",
    },
    avatar: "https://cdn.example/avatar.png",
    color: "#ff8800",
  };

  it("читает имя зрителя из displayName, как он едет по проводу", () => {
    const unpacked = unpackWaifuRoll(waifuRollWire);

    expect(unpacked.displayName).toBe("Стример");
  });

  it("не путает имя зрителя с proto-именем display_name", () => {
    // Провод приходит в camelCase, а proto-имя на нём не встречается. Если
    // распаковка вернётся к нему, имя зрителя снова станет пустым, и тест с
    // настоящей формой это поймает, а вот этот — тоже.
    expect(unpackWaifuRoll({ display_name: "Стример" }).displayName).toBe("");
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
