/**
 * Декодеры полезной нагрузки события оверлея.
 *
 * Сервер отдаёт protobuf-сообщение, сериализованное `System.Text.Json`, и
 * формат этот измерен, а не выведен из спецификации — см.
 * `tests/MARS.Alerts.Tests/Hubs/OverlayPayloadWireFormatTests.cs`. Отсюда три
 * особенности, без которых разбор не работает:
 *
 * 1. Ветки, у которых полезная нагрузка объявлена полем `bytes`
 *    (`message_json`, `user_json`, `gao_alert_json` и подобные), приезжают
 *    **массивом байт**. Их нужно собрать в `Uint8Array`, декодировать UTF-8 и
 *    только потом парсить. Ожидание base64 — по отображению protobuf — было бы
 *    неверным, и `JSON.parse` вернул бы из строки мусор.
 * 2. Незаполненные ветки `oneof` приезжают как `null`, а не отсутствуют:
 *    ключ есть у всех 36 веток, поэтому по наличию ключа событие не опознать.
 * 3. Имя ветки едет в camelCase (`newMessage`, `gaoAlert`), а само событие
 *    опознаётся по `eventCase`.
 *
 * Без декодера компонент тихо перестал бы получать сообщения: ошибка была бы
 * видна только на стенде, в виде пустого оверлея.
 */

/** Ветка события в том виде, как её отдаёт сервер. */
export type OverlayWireEvent = Record<string, unknown> & {
  eventCase?: string | number;
};

/**
 * Собирает ветку `oneof`: единственное значение, отличное от `null`.
 *
 * Возвращает `null`, если полезной нагрузки нет — так выглядит и ветка с
 * `EmptyEvent`, и ветка, которая не прислана вовсе.
 */
export function readBranch(payload: unknown): Record<string, unknown> | null {
  if (payload === null || typeof payload !== "object") {
    return null;
  }

  for (const [key, value] of Object.entries(payload)) {
    if (key === "eventCase") {
      continue;
    }

    if (value !== null && value !== undefined) {
      return value as Record<string, unknown>;
    }
  }

  return null;
}

/**
 * Декодирует поле, приехавшее массивом байт, в объект.
 *
 * Пустое поле даёт `undefined`, а не исключение: ветка с пустой нагрузкой
 * встречается в реальности, и её обработчик вправе решить, что делать сам.
 */
export function decodeBytesField(value: unknown): unknown {
  if (value === undefined || value === null) {
    return undefined;
  }

  // Массив байт — измеренная форма.
  if (Array.isArray(value)) {
    if (value.length === 0) {
      return undefined;
    }

    const decoded = new TextDecoder().decode(
      Uint8Array.from(value as number[])
    );

    return decoded.length > 0 ? (JSON.parse(decoded) as unknown) : undefined;
  }

  // Строка или объект: раньше `bytes` полагался base64-строкой, и такой вид
  // пришлось бы из старого кеша. Разбираем осторожно, чтобы не упасть на
  // мусоре, но и не проглотить его молча.
  if (typeof value === "string") {
    return value.length > 0 ? (JSON.parse(value) as unknown) : undefined;
  }

  return value;
}

/** Поле `bytes`, разобранное в объект. Пустое поле даёт `undefined`. */
export function decodeJsonBranch(payload: unknown, field: string): unknown {
  const branch = readBranch(payload);

  return branch === null ? undefined : decodeBytesField(branch[field]);
}

/**
 * Поле `repeated bytes`, разобранное в список объектов.
 *
 * Отдельная функция, потому что форма двухуровневая: снаружи список, внутри
 * каждый элемент — свой массив байт. Декодер одиночного поля на такой структуре
 * вернул бы мусор, и список тихо разъехался бы: первый элемент разобрался,
 * остальные нет.
 *
 * Пустое поле и пустой список дают `[]`, а не `undefined`: «список пришёл и
 * пуст» и «список не пришёл» — разные вещи, и обработчик вправе их различать.
 */
export function decodeJsonListBranch(
  payload: unknown,
  field: string
): unknown[] {
  const branch = readBranch(payload);

  if (branch === null) {
    return [];
  }

  const value = branch[field];

  if (!Array.isArray(value)) {
    return [];
  }

  const decoded: unknown[] = [];

  for (const item of value) {
    const one = decodeBytesField(item);

    if (one !== undefined) {
      decoded.push(one);
    }
  }

  return decoded;
}

/** Строковое поле ветки. Отсутствующее поле даёт `undefined`. */
export function readStringField(
  payload: unknown,
  field: string
): string | undefined {
  const branch = readBranch(payload);

  if (branch === null) {
    return undefined;
  }

  const value = branch[field];

  return typeof value === "string" ? value : undefined;
}

/** Числовое поле ветки. Отсутствующее поле даёт `undefined`. */
export function readNumberField(
  payload: unknown,
  field: string
): number | undefined {
  const branch = readBranch(payload);

  if (branch === null) {
    return undefined;
  }

  const value = branch[field];

  return typeof value === "number" ? value : undefined;
}

/**
 * Событие пришло: в ветке есть хоть что-то, кроме `eventCase`.
 *
 * Отличает «событие без данных» от «события не было»: первое приходит как
 * ветка с `EmptyEvent` и должно, например, включить анимацию.
 */
export function hasEvent(payload: unknown): boolean {
  return readBranch(payload) !== null;
}
