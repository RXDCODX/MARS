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
 /**
 * Ветка `oneof`, приехавшая от хаба.
 *
 * Возвращает полезную нагрузку события как есть — без поиска ветки внутри
 * объекта. Это соответствует тому, что кладёт на провод реле
 * `HubEventRelay`: `SendCoreAsync(имяМетода, [notification.Ветка])`, то есть
 * аргументом метода идёт **содержимое** ветки, а не конверт `TelegramusEvent`.
 *
 * Раньше здесь искалось единственное непустое поле среди тридцати шести ключей
 * конверта. При реальном проводе это означало, что декодер возвращал первое
 * поле самой ветки: для `{ id, messageJson }` — строку `"42"`, для
 * `{ seconds }` — число `30`. Из тридцати шести событий работало шесть без
 * данных и несколько случайно, остальные молча ничего не показывали. Тест
 * формата это пропустил, потому что сериализовал конверт, а не то, что уходит
 * в метод хаба.
 *
 * Возвращается `null` для не-объектов и для `null`, а не исключение: ветка
 * может прийти пустой, и разбираться с этим должен обработчик.
 *
 * Тип не задаётся обобщением намеренно: в файлах `.tsx` TypeScript разбирает
 * вызов `readBranch<MediaDto>(payload)` как сравнение — `payload` уезжает в
 * правую часть. Вызывающий приводит тип сам, через `as unknown as Dto`, и это
 * приведение видно глазами.
 */
export function readBranch(payload: unknown): Record<string, unknown> | null {
  if (
    payload === null ||
    typeof payload !== "object" ||
    Array.isArray(payload)
  ) {
    return null;
  }

  return payload as Record<string, unknown>;
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
