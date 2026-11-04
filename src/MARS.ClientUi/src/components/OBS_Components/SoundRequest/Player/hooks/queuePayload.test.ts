import { describe, expect, it } from "vitest";

import { readQueueItems } from "./queuePayload";

/**
 * Разбор очереди звуковых запросов.
 *
 * Хаб отдаёт proto-сообщение, а компонент читает REST-DTO, и в треке поле
 * называется по-разному: в `sound_request.proto` это `duration_seconds`, в
 * контракте клиента — `duration` строкой `hh:mm:ss`. Без приведения полоса
 * длительности у всех треков очереди показывала «00:00».
 */
describe("разбор очереди звуковых запросов", () => {
  // QueueItemSnapshot в том виде, в каком он едет в метод QueueChanged:
  // AddMarsSignalR ставит camelCase, поэтому track_name из proto приходит как
  // trackName, а duration_seconds — как durationSeconds.
  const protoItem = {
    id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    trackId: "11111111-2222-3333-4444-555555555555",
    track: {
      id: "11111111-2222-3333-4444-555555555555",
      trackName: "Хижина в лесу",
      durationSeconds: 245,
      url: "https://youtu.be/abc",
    },
    queueOrder: 1,
    requestedByTwitchId: "42",
    requestedAt: "2026-10-03T00:00:00Z",
  };

  it("приводит proto-трек к REST-форме", () => {
    const items = readQueueItems({ queue: [protoItem] });

    expect(items).not.toBeNull();
    expect(items?.[0].track).toMatchObject({
      id: "11111111-2222-3333-4444-555555555555",
      trackName: "Хижина в лесу",
      // Без этого полоса длительности у всех треков показывала «00:00».
      duration: "00:04:05",
    });
  });

  it("не теряет остальные поля элемента", () => {
    const items = readQueueItems({ queue: [protoItem] });
    const first = items?.[0];

    expect(first?.id).toBe(protoItem.id);
    expect(first?.queueOrder).toBe(1);
    expect(first?.requestedByTwitchId).toBe("42");
  });

  it("уже приведённый REST-трек не портится", () => {
    // Тот же элемент может прийти из REST-запроса, и там поле называется
    // duration. Приведение не должно его затирать.
    const restItem = {
      id: protoItem.id,
      queueOrder: 1,
      track: {
        id: "track-1",
        trackName: "Хижина в лесу",
        duration: "00:04:05",
      },
    };

    const items = readQueueItems({ queue: [restItem] });

    expect(items?.[0]?.track?.duration).toBe("00:04:05");
  });

  it("пустая очередь остаётся пустой", () => {
    expect(readQueueItems({ queue: [] })).toEqual([]);
  });

  it("отсутствие очереди даёт null, а не пустой список", () => {
    expect(readQueueItems({})).toBeNull();
    expect(readQueueItems(null)).toBeNull();
  });
});
