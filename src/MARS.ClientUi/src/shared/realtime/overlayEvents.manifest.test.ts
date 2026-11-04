import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { OVERLAY_EVENT_NAMES } from "./overlayEvents";

/**
 * Контракт оверлея сверяется с манифестом на стороне C#.
 *
 * Имя метода хаба приходит строкой в вызове SignalR, и TypeScript её не
 * проверяет: `connection.on("credits", …)` и `connection.on("Credits", …)`
 * одинаково компилируются, а событие при этом не доходит. Именно на этом
 * держались расхождения в клиенте монолита — `updatewaifuprizes` против
 * `UpdateWaifuPrizes`, `TunaMusicInfo` против `SendPlayerData`/`BeYm`.
 *
 * Манифест `src/MARS.Alerts/Hubs/overlay-hub.manifest.json` сверяется ещё и
 * тестом на C# — с интерфейсом `ITelegramusHub` и с ветками `oneof` из
 * `telegramus.proto`. Поэтому цепочка одна: proto → интерфейс → манифест →
 * карта типов здесь.
 *
 * Путь ведёт за пределы ClientApp намеренно: манифест лежит рядом с
 * интерфейсом хаба, а не в клиенте. Копия контракта в клиенте была бы второй
 * правдой, которая разъехалась бы с сервером при первом же новом событии.
 * Сборку образа это не затрагивает — vitest в образе не запускается, а
 * проверка контракта живёт в CI-шаге фронтенда, где репозиторий доступен целиком.
 */
const manifestPath = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../../../../MARS.Alerts/Hubs/overlay-hub.manifest.json"
);

const manifest = JSON.parse(readFileSync(manifestPath, "utf8")) as string[];

describe("контракт оверлея совпадает с манифестом хаба", () => {
  it("manifest-файл найден по ожидаемому пути", () => {
    // Путь к контракту — тоже контракт. Стоит переехать папке, и сверка
    // молча перестала бы читать файл, если бы не эта проверка.
    // Разделители нормализуются: на Windows путь приходит с обратными слэшами,
    // и сравнение с POSIX-строкой всегда было бы ложным.
    expect(manifestPath.replace(/\\/g, "/")).toMatch(
      /src\/MARS\.Alerts\/Hubs\/overlay-hub\.manifest\.json$/
    );
    expect(manifest.length).toBeGreaterThan(0);
  });

  it("набор имён совпадает целиком", () => {
    // Сравнение множеств, а не порядка: порядок манифеста следует номерам полей
    // в proto и для контракта не значим. Расхождение множеств означало бы
    // забытое или лишнее событие.
    expect([...OVERLAY_EVENT_NAMES].sort()).toEqual([...manifest].sort());
  });

  it("в манифесте 36 событий", () => {
    expect(manifest).toHaveLength(36);
  });

  it("имени нет дважды", () => {
    const duplicates = manifest.filter(
      (name, index) => manifest.indexOf(name) !== index
    );

    expect(duplicates).toEqual([]);
  });
});
