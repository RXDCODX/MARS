import { describe, expect, it } from "vitest";

import { readTunaMusic } from "./useTunaEvent";

/**
 * Разбор события хаба информации о треке.
 *
 * Поле прогресса называется по-разному в двух формах одного и того же трека: в
 * REST-контракте это `progress`, а в proto — `progression`. Хаб отдаёт proto, и
 * компонент читал `track.progress`, то есть получал `undefined` и начинал полосу
 * с нуля вместо фактической позиции.
 *
 * Разбор здесь, а не в компоненте: одно поле, две формы, и подставлять нужное
 * имя в каждом потребителе значило бы забыть где-нибудь.
 */
describe("разбор события трека", () => {
  // TunaTrack из tuna.proto, как он едет в метод хаба.
  const hubTrack = {
    id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    cover: "https://cdn.example/cover.png",
    title: "Kasane Teto",
    artists: ["siIvaGunner"],
    status: "playing",
    progression: 90,
    duration: 245,
    // Через camelCase поле приходит как albumUrl: в proto album_url, а хаб пишет
    // именем, который задаёт AddMarsSignalR. Клиентский контракт при этом ждёт
    // album_url — расхождение тех же двух форм, что и с прогрессом.
    albumUrl: "https://cdn.example/album.png",
  };

  it("читает прогресс из progression, как он назван в proto", () => {
    const music = readTunaMusic({ data: hubTrack, hostname: "tuna" });

    expect(music).not.toBeNull();
    expect((music?.data as Record<string, unknown>).progress).toBe(90);
  });

  it("не теряет остальные поля трека", () => {
    const music = readTunaMusic({ data: hubTrack, hostname: "tuna" });
    const data = music?.data as Record<string, unknown>;

    expect(data.title).toBe("Kasane Teto");
    expect(data.duration).toBe(245);
    expect(data.status).toBe("playing");
    expect(data.artists).toEqual(["siIvaGunner"]);
    expect(music?.hostname).toBe("tuna");
  });

  it("принимает и REST-форму с progress", () => {
    // Тот же трек может прийти из другого источника, и там поле называется
    // progress. Подмена значения не должна его терять.
    const music = readTunaMusic({
      data: { ...hubTrack, progress: 30, progression: undefined },
      hostname: "tuna",
    });

    expect((music?.data as Record<string, unknown>).progress).toBe(30);
  });

  it("нулевой прогресс остаётся нулём, а не исчезает", () => {
    // «нулевого прогресса нет» и «прогресс нулевой» — разные вещи: первое
    // означает, что позиция неизвестна, второе что трек в начале.
    const music = readTunaMusic({
      data: { ...hubTrack, progression: 0 },
      hostname: "tuna",
    });

    expect((music?.data as Record<string, unknown>).progress).toBe(0);
  });

  it("приводит albumUrl к album_url, как их читает контракт клиента", () => {
    // Второе расхождение тех же двух форм. На проводе поле называется albumUrl
    // (camelCase от album_url в proto), а контракт клиента ждёт album_url.
    const music = readTunaMusic({ data: hubTrack, hostname: "tuna" });

    expect((music?.data as Record<string, unknown>).album_url).toBe(
      "https://cdn.example/album.png"
    );
  });

  it("событие без данных пропускается", () => {
    expect(readTunaMusic({ hostname: "tuna" })).toBeNull();
    expect(readTunaMusic(null)).toBeNull();
  });

  it("событие с пустыми данными пропускается так же", () => {
    // Отбрасывался только `undefined`, а `data: null` проходил дальше: в редьюсере
    // `key(data)` возвращала пустую строку, и трек на экране тихо сменялся на
    // пустой. Формы данных различаются только тем, как сервер их заполнил.
    expect(readTunaMusic({ data: null, hostname: "tuna" })).toBeNull();
    expect(readTunaMusic({ data: undefined, hostname: "tuna" })).toBeNull();
  });
});
