import { describe, expect, it, test } from "vitest";

const moduleLoaders = import.meta.glob([
  "./**/*.ts",
  "./**/*.tsx",
  "!./**/*.stories.ts",
  "!./**/*.stories.tsx",
  "!./**/*.test.ts",
  "!./**/*.test.tsx",
  "!./**/*.d.ts",
  "!./**/index.ts",
]);

const moduleEntries = Object.entries(moduleLoaders);

/**
 * Ошибки, которые допустимы при импорте модуля оверлея.
 *
 * Здесь не должно быть ни одного адреса хаба. Раньше стояли маркеры
 * `Cannot resolve 'undefinedhubs/telegramus'` и `.../scoreboard'`: они появились
 * из-за сборки адреса на верхнем уровне модуля, и `import.meta.env
 * .VITE_BASE_PATH` в vitest давал `undefined`. Такой allow-list скрывал ровно ту
 * регрессию, ради которой его и создавали, — адрес теперь собирается
 * `resolveHubUrl` в момент вызова, поэтому повторение ошибки обязано ронять
 * тест.
 *
 * Оставшиеся два маркера относятся к холсту: он есть в jsdom лишь как заглушка,
 * и код оверлея в него пишет.
 */
const allowedErrorMarkers = [
  "Cannot set properties of null (setting 'fillStyle')",
  "HTMLCanvasElement's getContext() method",
];

const isAllowedImportSideEffectError = (error: unknown): boolean => {
  const message = error instanceof Error ? error.message : String(error);
  return allowedErrorMarkers.some(marker => message.includes(marker));
};

describe("OBS_Components smoke coverage", () => {
  it("finds files to validate", () => {
    expect(moduleEntries.length).toBeGreaterThan(0);
  });

  test.each(moduleEntries)("imports %s", async (_path, loader) => {
    try {
      const moduleExports = await (loader as () => Promise<unknown>)();
      expect(moduleExports).toBeTruthy();
    } catch (error) {
      if (isAllowedImportSideEffectError(error)) {
        expect(true).toBe(true);
      } else {
        throw error;
      }
    }
  });
});
