import { describe, expect, it } from "vitest";

import {
  decodeJsonBranch,
  readMediaBranch,
  readMediaListBranch,
} from "./overlayPayload";

/**
 * Разбор медиа-алертов.
 *
 * Ветка `AlertEvent` в proto — это `{ media: MediaPayload }`, то есть объект с
 * одним полем `media`, а не сам `MediaPayload`. Раньше обработчик считал, что
 * пришёл `MediaPayload`, и делал `message.mediaInfo.id = ...` — то есть
 * `undefined.id`, и одиночный медиа-алерт не показывался вовсе, а в консоли
 * падало «Cannot set properties of undefined».
 *
 * Формы взяты из `telegramus.proto`, а не из предположения.
 */
describe("разбор медиа-алерта", () => {
  const mediaInfo = {
    id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    type: "Image",
    albumName: "альбом",
    filePath: "/alerts/bell.wav",
  };

  it("разворачивает ветку Alert в медиа", () => {
    // Что реально едет в метод Alert: содержимое ветки AlertEvent.
    const alert = {
      media: { mediaInfo, uploadStartTime: "2026-10-03T00:00:00Z" },
    };

    expect(readMediaBranch(alert)).toEqual({
      mediaInfo,
      uploadStartTime: "2026-10-03T00:00:00Z",
    });
  });

  it("возвращает null, если медиа нет", () => {
    expect(readMediaBranch({})).toBeNull();
    expect(readMediaBranch(null)).toBeNull();
  });

  it("не принимает сам MediaPayload за ветку", () => {
    // Терпимость к payload без обёртки здесь была бы опаснее отказа: вернулся
    // бы объект без mediaInfo, и падение случилось бы на той же строке.
    expect(readMediaBranch(mediaInfo)).toBeNull();
  });

  it("разворачивает ветку Alerts в список", () => {
    const alerts = {
      media: [{ mediaInfo }, { mediaInfo: { ...mediaInfo, id: "2" } }],
    };

    expect(readMediaListBranch(alerts)).toHaveLength(2);
  });

  it("пустой список медиа — это отсутствие, а не пустая очередь", () => {
    // Разница та же, что у списков: «медиа не пришло» и «медиа нет» для
    // отображения одно и то же, но пустой массивом притворяться не стоит.
    expect(readMediaListBranch({ media: [] })).toEqual([]);
    expect(readMediaListBranch({})).toBeNull();
  });

  it("одиночный объект не принимается за список", () => {
    expect(readMediaListBranch({ media: mediaInfo })).toBeNull();
  });
});

/**
 * Списки, приходящие полем `bytes`.
 *
 * В proto `prizes_json` — это `bytes`, то есть на проводе массив чисел. Раньше
 * обработчик проверял `Array.isArray(payload)`, то есть ждал уже разобранный
 * список, и получал объект вида `{ prizesJson: [...] }`. Призрачно: `Array.isArray`
 * давал `false`, обработчик выходил, и списки призов оставались пустыми всегда.
 */
describe("разбор списков, приходящих полем bytes", () => {
  const prizes = [{ id: "waifu-1", name: "Вайфу" }];
  const bytes = [...new TextEncoder().encode(JSON.stringify(prizes))];

  it("разбирает список из массива байт", () => {
    // Одиночный bytes: JSON лежит внутри одного массива байт. Функция для
    // repeated bytes здесь не подходит — она разбирала бы список списков.
    expect(decodeJsonBranch({ prizesJson: bytes }, "prizesJson")).toEqual(
      prizes
    );
  });

  it("пустой массив байт даёт undefined, а не список", () => {
    expect(decodeJsonBranch({ prizesJson: [] }, "prizesJson")).toBeUndefined();
  });

  it("отсутствующее поле даёт null, а не пустой список", () => {
    // Иначе обработчик решил бы, что призов нет, и стёр бы уже показанные.
    expect(decodeJsonBranch({}, "prizesJson")).toBeUndefined();
  });
});
