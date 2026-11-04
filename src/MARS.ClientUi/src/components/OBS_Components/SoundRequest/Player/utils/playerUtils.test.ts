import { describe, expect, it } from "vitest";

import { parseDurationToSeconds } from "./playerUtils";

/**
 * Разбор длительности в десктопном плеере.
 *
 * Функция зовётся из `PlayerToolbar` и `useTrackProgress`, и оба получают её из
 * хаба очереди звуковых запросов. Хаб отдаёт длительность строкой `hh:mm:ss`, а
 * парсер понимал только ISO `PT…` — то есть отдавал 0, и полоса прогресса на
 * `/player` не двигалась никогда.
 *
 * Второй парсер с тем же именем есть в `VideoScreen/utils/parseDuration.ts`, и он
 * понимает оба формата. Расхождение двух реализаций под одним именем и было
 * причиной: видеоэкран починили, десктоп — нет.
 */
describe("разбор длительности трека", () => {
  it("разбирает hh:mm:ss, как отдаёт хаб", () => {
    expect(parseDurationToSeconds("00:04:05")).toBe(245);
    expect(parseDurationToSeconds("01:00:00")).toBe(3600);
  });

  it("разбирает mm:ss", () => {
    expect(parseDurationToSeconds("04:05")).toBe(245);
  });

  it("по-прежнему разбирает ISO 8601 из REST", () => {
    // Длительность из REST-контракта приходит в ISO, и смена формата не должна
    // ломать то, что работало.
    expect(parseDurationToSeconds("PT1M23S")).toBe(83);
    expect(parseDurationToSeconds("PT2H3M45S")).toBe(7425);
  });

  it("пустое и отсутствующее значение дают ноль", () => {
    expect(parseDurationToSeconds(undefined)).toBe(0);
    expect(parseDurationToSeconds("")).toBe(0);
  });

  it("мусор даёт ноль, а не NaN", () => {
    // Полоса прогресса считает деление на длительность, и NaN ушёл бы в
    // отрисовку как «-1%».
    expect(parseDurationToSeconds("не время")).toBe(0);
    expect(parseDurationToSeconds("00:xx:00")).toBe(0);
  });
});
