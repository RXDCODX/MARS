import { describe, expect, it } from "vitest";

import {
  decodeJsonBranch,
  hasEvent,
  readBranch,
  readNumberField,
  readStringField,
} from "./overlayPayload";

/**
 * Разбор полезной нагрузки события оверлея.
 *
 * Примеры здесь — не выдумки, а фактический вывод сериализатора сервера,
 * снятый тестом `OverlayPayloadWireFormatTests` на стороне C#. Если формат
 * поменяется, тесты разойдутся, и это лучше, чем тихая поломка оверлея на
 * стенде.
 *
 * Ключевое, что видно из примеров: все 36 веток `oneof` присутствуют, но
 * незаполненные равны `null`, а ветки с `bytes` приходят массивом чисел, а не
 * строкой. И то и другое ломает наивный разбор.
 */
describe("разбор полезной нагрузки события", () => {
  /** Настоящее сообщение сервера: newMessage заполнена, остальные ветки null. */
  const newMessageEvent = {
    alert: null,
    alerts: null,
    waifuRoll: null,
    addNewWaifu: null,
    showCurrentWife: null,
    mergeWaifu: null,
    updateWaifuPrizes: null,
    fumoFriday: null,
    newMessage: {
      id: "42",
      // {"text":"привет"} в UTF-8, побайтово. Кириллица — два байта на
      // букву, и ошибка в этой константе выдаёт мусор вместо текста, что
      // и случилось при первом прогоне.
      messageJson: [
        123, 34, 116, 101, 120, 116, 34, 58, 34, 208, 191, 209, 128, 208, 184,
        208, 178, 208, 181, 209, 130, 34, 125,
      ],
    },
    deleteMessage: null,
    highlite: null,
    postTwitchInfo: null,
    makeScreenParticles: null,
    makeScreenEmojisParticles: null,
    randomMem: null,
    autoMessage: null,
    adhd: null,
    explosion: null,
    leroyAlert: null,
    gaoAlert: null,
    credits: null,
    michaelJackson: null,
    mikuMonday: null,
    mikuMikuBeam: null,
    phonkEdit: null,
    tikTokEdit: null,
    allRefund: null,
    audioQuizStart: null,
    audioQuizStop: null,
    fumoRoll: null,
    updateFumoPrizes: null,
    frogRoll: null,
    updateFrogPrizes: null,
    mikuRoll: null,
    updateMikuPrizes: null,
    adhdConfig: null,
    eventCase: 9,
  };

  it("находит заполненную ветку среди тридцати пяти пустых", () => {
    const branch = readBranch(newMessageEvent);

    expect(branch).not.toBeNull();
    expect(branch?.id).toBe("42");
  });

  it("не путает eventCase с веткой события", () => {
    // eventCase присутствует всегда и числом, но веткой не является: если бы
    // он попал в разбор, обработчик получил бы номер вместо данных события.
    expect(readBranch(newMessageEvent)).not.toHaveProperty("eventCase");
  });

  it("декодирует поле bytes из массива в объект", () => {
    const decoded = decodeJsonBranch(newMessageEvent, "messageJson");

    expect(decoded).toEqual({ text: "привет" });
  });

  it("читает обычные строковые поля без декодирования", () => {
    expect(readStringField(newMessageEvent, "id")).toBe("42");
  });

  it("возвращает undefined для поля, которого нет", () => {
    // Раньше такие случаи молча давали null и обработчик падал на разборе.
    expect(readStringField(newMessageEvent, "missing")).toBeUndefined();
    expect(decodeJsonBranch(newMessageEvent, "missing")).toBeUndefined();
  });

  it("различает ветку без данных и отсутствие события", () => {
    // Explosion — ветка с EmptyEvent: событие было, данных в нём нет.
    const explosion = { explosion: {}, eventCase: 18 };

    expect(hasEvent(explosion)).toBe(true);
    expect(hasEvent({ explosion: null, credits: null })).toBe(false);
    expect(hasEvent(null)).toBe(false);
    expect(hasEvent(undefined)).toBe(false);
  });

  it("читает числа как числа", () => {
    const adhd = { adhd: { seconds: 30 }, eventCase: 17 };

    expect(readNumberField(adhd, "seconds")).toBe(30);
  });

  it("не принимает строку за число", () => {
    // Число приходит числом. Если бы пришло строкой, таймер ADHD получил бы
    // «30» и тихо не отсчитал бы время.
    const adhd = { adhd: { seconds: "30" }, eventCase: 17 };

    expect(readNumberField(adhd, "seconds")).toBeUndefined();
  });

  it("пустое поле bytes даёт undefined, а не исключение", () => {
    const empty = { allRefund: { userJson: [] }, eventCase: 27 };

    expect(decodeJsonBranch(empty, "userJson")).toBeUndefined();
  });
});
