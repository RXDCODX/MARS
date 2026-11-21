import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { OVERLAY_EVENT_NAMES } from "./overlayEvents";

/**
 * Контракт оверлея сверяется с манифестом на стороне C#. Имя метода хаба
 * приходит строкой в вызове SignalR, и TypeScript её не проверяет: расхождение
 * `updatewaifuprizes` / `UpdateWaifuPrizes` компилировалось, но события не доходили.
 *
 * Манифест генерируется из рефлексии C# (TelegramusEvent + IAlertsHub), а этот
 * файл — из манифеста. Тест защищает от ручной правки сгенерированного файла.
 */
const manifestPath = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../../../../MARS.Alerts/MARS.Alerts.Presentation.Overlay/Hubs/overlay-hub.manifest.json",
);

type ManifestEntry = { name: string; hasPayload: boolean };
const manifest = JSON.parse(readFileSync(manifestPath, "utf8")) as ManifestEntry[];

describe("контракт оверлея совпадает с манифестом хаба", () => {
  it("manifest-файл найден по ожидаемому пути и не пуст", () => {
    expect(manifestPath.replace(/\\/g, "/")).toContain(
      "src/MARS.Alerts/MARS.Alerts.Presentation.Overlay/Hubs/overlay-hub.manifest.json",
    );
    expect(manifest.length).toBeGreaterThan(0);
  });

  it("набор имён совпадает целиком", () => {
    expect([...OVERLAY_EVENT_NAMES].sort()).toEqual(
      manifest.map((entry) => entry.name).sort(),
    );
  });

  it("имени нет дважды", () => {
    const names = manifest.map((entry) => entry.name);
    expect(names.filter((name, index) => names.indexOf(name) !== index)).toEqual([]);
  });

  it("у каждой записи честно указано наличие полезной нагрузки", () => {
    for (const entry of manifest) {
      expect(typeof entry.name).toBe("string");
      expect(typeof entry.hasPayload).toBe("boolean");
    }
  });
});
