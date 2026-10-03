import path from "node:path";
import { fileURLToPath } from "node:url";

import { storybookTest } from "@storybook/addon-vitest/vitest-plugin";
import { playwright } from "@vitest/browser-playwright";
import tsconfigPaths from "vite-tsconfig-paths";
import { defineConfig } from "vitest/config";

const dirname =
  typeof __dirname !== "undefined"
    ? __dirname
    : path.dirname(fileURLToPath(import.meta.url));

/**
 * Таймаут одного теста.
 *
 * Умолчание vitest — 5 секунд, и три теста в этом проекте в него не укладываются:
 * они импортируют большие деревья модулей (RoutesPage, WaifuRollPage,
 * OBSComponentsSmokeCoverage), и на этой машине импорт занимает около 6 секунд.
 * То есть падал не тест, а трансформация, и результат зависел от скорости
 * машины: на CI тот же код мог пройти, а локально падал.
 *
 * Значение не «просто побольше»: 15 секунд заведомо не хватает зависшему тесту,
 * поэтому ожидание всё ещё ловит зависание — в отличие от `testTimeout: 0`,
 * который отключил бы его совсем.
 */
const UNIT_TEST_TIMEOUT_MS = 15000;

export default defineConfig(() => {
  const plugins = [tsconfigPaths()];
  const includeStorybook = !process.env.SKIP_STORYBOOK_VITEST;

  return {
    plugins,
    resolve: {
      alias: {
        "@": path.resolve(dirname, "src"),
        "@/components": path.resolve(dirname, "src/components"),
        "@/Site": path.resolve(dirname, "src/Site"),
        "@/shared": path.resolve(dirname, "src/shared"),
        "@/contexts": path.resolve(dirname, "src/contexts"),
        "@/routes": path.resolve(dirname, "src/routes"),
        "@/app": path.resolve(dirname, "src/app"),
        "@/assets": path.resolve(dirname, "src/assets"),
        "@/styles": path.resolve(dirname, "src/styles"),
        "@/utils": path.resolve(dirname, "src/shared/Utils"),
        "@/api": path.resolve(dirname, "src/shared/api"),
        "@/types": path.resolve(dirname, "src/shared/types"),
      },
    },
    test: {
      projects: includeStorybook
        ? [
            {
              extends: true,
              test: {
                name: "unit",
                environment: "jsdom",
                include: ["src/**/*.test.{ts,tsx}", "src/**/*.spec.{ts,tsx}"],
                setupFiles: ["src/tests/vitest.setup.ts"],
                testTimeout: UNIT_TEST_TIMEOUT_MS,
              },
            },
            {
              extends: true,
              plugins: [
                storybookTest({
                  configDir: path.join(dirname, ".storybook"),
                }),
              ],
              test: {
                name: "storybook",
                browser: {
                  enabled: true,
                  headless: true,
                  provider: playwright(),
                  instances: [
                    {
                      browser: "chromium",
                    },
                  ],
                },
                setupFiles: [".storybook/vitest.setup.ts"],
              },
            },
          ]
        : [
            {
              extends: true,
              test: {
                name: "unit",
                environment: "jsdom",
                include: ["src/**/*.test.{ts,tsx}", "src/**/*.spec.{ts,tsx}"],
                setupFiles: ["src/tests/vitest.setup.ts"],
                testTimeout: UNIT_TEST_TIMEOUT_MS,
              },
            },
          ],
    },
  };
});
