import { describe, expect, it } from "vitest";

import {
  decodeBytesField,
  decodeJsonBranch,
  decodeJsonListBranch,
  readBranch,
  readNumberField,
  readStringField,
} from "./overlayPayload";

/**
 * Разбор полезной нагрузки события оверлея.
 *
 * Примеры здесь — не выдумки, а фактический вывод сериализатора сервера,
 * снятый тестом `OverlayPayloadWireFormatTests` на стороне C#.
 *
 * Ключевое, что видно из примеров: в метод хаба уходит **содержимое** ветки,
 * а не конверт `TelegramusEvent`. Реле вызывает
 * `SendCoreAsync(имяМетода, [notification.NewMessage])`, поэтому браузер
 * получает `{ id, messageJson }`, а не объект с тридцатью шестью ключами.
 *
 * Раньше фикстуры здесь были конвертами, и декодер искал ветку внутри объекта
 * по единственному непустому полю. На реальном проводе он возвращал первое поле
 * самой ветки: для `{ id, messageJson }` — строку `"42"`. Работали шесть
 * событий без данных и несколько случайно, остальные молча ничего не
 * показывали. Тест на C# это пропускал, потому что тоже мерил конверт; теперь
 * он мерит именно аргумент реле.
 *
 * Второе, что видно из примеров: поля `bytes` приходят массивом чисел, а не
 * строкой. Ошибка в этой форме даёт мусор вместо текста.
 */
describe("разбор полезной нагрузки события", () => {
  /** Что приходит в метод NewMessage: содержимое ветки, без конверта. */
  const newMessageEvent = {
    id: "42",
    // {"text":"привет"} в UTF-8, побайтово. Кириллица — два байта на
    // букву, и ошибка в этой константе выдаёт мусор вместо текста.
    messageJson: [
      123, 34, 116, 101, 120, 116, 34, 58, 34, 208, 191, 209, 128, 208, 184,
      208, 178, 208, 181, 209, 130, 34, 125,
    ],
  };

  it("отдаёт содержимое ветки как есть", () => {
    const branch = readBranch(newMessageEvent);

    expect(branch).not.toBeNull();
    expect(branch?.id).toBe("42");
    expect(branch?.messageJson).toBe(newMessageEvent.messageJson);
  });

  it("не выдаёт за ветку поле, которого в ней нет", () => {
    // Конверта на проводе нет вовсе, значит и ключа eventCase быть не может.
    // Если бы он появился, разбор принял бы номер события за данные.
    expect(readBranch(newMessageEvent)).not.toHaveProperty("eventCase");
  });

  it("не путает конверт с содержимым ветки", () => {
    // Конверт приходит только из REST-ответов и из других клиентов. Если
    // декодер начнёт искать в нём ветку, событие снова разберётся неверно.
    const envelope = {
      alert: null,
      newMessage: newMessageEvent,
      eventCase: 9,
    };

    expect(readBranch(envelope)?.id).toBeUndefined();
  });

  it("не принимает за ветку массив и скаляр", () => {
    // Аргумент события без данных — пустой объект, а не массив и не число.
    expect(readBranch([])).toBeNull();
    expect(readBranch(30)).toBeNull();
    expect(readBranch("42")).toBeNull();
    expect(readBranch(null)).toBeNull();
    expect(readBranch({})).not.toBeNull();
  });

  it("декодирует поле bytes из массива в объект", () => {
    expect(decodeJsonBranch(newMessageEvent, "messageJson")).toEqual({
      text: "привет",
    });
  });

  it("читает обычные строковые поля без декодирования", () => {
    expect(readStringField(newMessageEvent, "id")).toBe("42");
  });

  it("возвращает undefined для поля, которого нет", () => {
    // Раньше такие случаи молча давали null, и обработчик падал на разборе.
    expect(readStringField(newMessageEvent, "missing")).toBeUndefined();
    expect(decodeJsonBranch(newMessageEvent, "missing")).toBeUndefined();
  });

  it("читает числа как числа", () => {
    // Событие Adhd приходит как { seconds: 30 }.
    expect(readNumberField({ seconds: 30 }, "seconds")).toBe(30);
  });

  it("не принимает строку за число", () => {
    // Число приходит числом. Если бы пришло строкой, таймер ADHD получил бы
    // «30» и тихо не отсчитал бы время.
    expect(readNumberField({ seconds: "30" }, "seconds")).toBeUndefined();
  });

  it("пустое поле bytes даёт undefined, а не исключение", () => {
    // Ветка AllRefund приходит как { userJson: [] }.
    expect(decodeJsonBranch({ userJson: [] }, "userJson")).toBeUndefined();
  });

  it("битый JSON в поле bytes не роняет обработчик", () => {
    // Один битый байт с сервера уводил компонент оверлея в ErrorBoundary:
    // JSON.parse бросал SyntaxError прямо в обработчик события.
    const garbage = [123, 34, 0, 255, 254, 34, 125];

    expect(decodeJsonBranch({ userJson: garbage }, "userJson")).toBeUndefined();
    expect(decodeBytesField(garbage)).toBeUndefined();
  });

  it("битая строка вместо JSON не роняет обработчик", () => {
    expect(decodeBytesField("{ это не json")).toBeUndefined();
    expect(decodeBytesField("")).toBeUndefined();
  });

  it("корректный JSON по-прежнему разбирается", () => {
    // Защита не должна была превратить разбор в «всегда undefined».
    const bytes = [...new TextEncoder().encode('{"text":"привет"}')];

    expect(decodeBytesField(bytes)).toEqual({ text: "привет" });
    expect(decodeBytesField('{"text":"привет"}')).toEqual({ text: "привет" });
  });

  it("декодирует поле repeated bytes — список списков", () => {
    // В proto это repeated bytes, то есть снаружи список, внутри — массив байт
    // на каждый элемент. Декодер одиночного поля на такой форме разобрал бы
    // первый элемент и молча потерял остальные.
    const event = {
      // {"id":"a"} и {"id":"b"} в UTF-8.
      usersJson: [
        [123, 34, 105, 100, 34, 58, 34, 97, 34, 125],
        [123, 34, 105, 100, 34, 58, 34, 98, 34, 125],
      ],
    };

    expect(decodeJsonListBranch(event, "usersJson")).toEqual([
      { id: "a" },
      { id: "b" },
    ]);
  });

  it("отсутствующий список даёт пустой список, а не undefined", () => {
    // «Список не пришёл» и «список пуст» — разные вещи: первое означает, что
    // события не было, второе — что зрителей нет.
    expect(decodeJsonListBranch({ credits: {} }, "usersJson")).toEqual([]);
    expect(decodeJsonListBranch(null, "usersJson")).toEqual([]);
  });
});
