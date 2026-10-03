/**
 * Распаковка вайфу-событий из содержимого ветки `oneof`.
 *
 * Формы взяты из `telegramus.proto`, а не из предположения о том, что прислал
 * сервис:
 *
 * - `WaifuRollEvent` и `AddNewWaifuEvent`: `waifu`, `display_name`, `color`;
 * - `ShowCurrentWifeEvent` и `MergeWaifuEvent`: `waifu`, `husband`, `avatar`,
 *   `color`.
 *
 * Раньше распаковка ждала `host` и `twitchUser`, которых в протоколе нет
 * вовсе: имя зрителя бралось как `host?.twitchUser?.displayName`, то есть всегда
 * было пустой строкой, и подпись под вайфу не появлялась ни разу.
 *
 * Вынесено отдельным модулем, а не внутрь стора: разбор формы проверяется тестом
 * напрямую, без поднятия zustand и без заглушек.
 */

/** `WaifuInfo` на проводе: четыре поля, а не полная сущность вайфу. */
export interface WaifuInfoWire {
  id?: string;
  name?: string;
  image_url?: string;
  source?: string;
}

/** `HusbandInfo` на проводе. */
export interface HusbandInfoWire {
  id?: string;
  display_name?: string;
  avatar_url?: string;
}

export interface WaifuRollUnpacked {
  waifu: WaifuInfoWire | null;
  displayName: string;
  color: string;
}

export interface WaifuHusbandUnpacked {
  waifu: WaifuInfoWire | null;
  husband: HusbandInfoWire | null;
  color: string;
}

const asRecord = (payload: unknown): Record<string, unknown> =>
  payload !== null && typeof payload === "object" && !Array.isArray(payload)
    ? (payload as Record<string, unknown>)
    : {};

const asString = (value: unknown): string =>
  typeof value === "string" ? value : "";

/** Распаковка ветки, где зритель назван по имени. */
export function unpackWaifuRoll(payload: unknown): WaifuRollUnpacked {
  const branch = asRecord(payload);
  const waifu = branch.waifu;

  return {
    waifu:
      waifu !== null && typeof waifu === "object" && !Array.isArray(waifu)
        ? (waifu as WaifuInfoWire)
        : null,
    displayName: asString(branch.display_name),
    color: asString(branch.color),
  };
}

/** Распаковка ветки, где зритель приходит объектом `husband`. */
export function unpackWaifuHusband(payload: unknown): WaifuHusbandUnpacked {
  const branch = asRecord(payload);
  const waifu = branch.waifu;
  const husband = branch.husband;

  return {
    waifu:
      waifu !== null && typeof waifu === "object" && !Array.isArray(waifu)
        ? (waifu as WaifuInfoWire)
        : null,
    husband:
      husband !== null && typeof husband === "object" && !Array.isArray(husband)
        ? (husband as HusbandInfoWire)
        : null,
    color: asString(branch.color),
  };
}
