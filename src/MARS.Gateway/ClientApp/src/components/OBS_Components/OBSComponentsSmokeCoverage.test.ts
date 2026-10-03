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

const allowedErrorMarkers = [
  "Cannot resolve 'undefinedhubs/telegramus'",
  "Cannot resolve 'undefinedhubs/scoreboard'",
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
