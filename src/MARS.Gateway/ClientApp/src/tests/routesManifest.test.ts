import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { allRoutes } from "@/routes/config/allRoutes";

/**
 * Маршруты клиента в файле для навигационных тестов.
 *
 * Файл порождается из того же `allRoutes`, которым собирается роутер, и не
 * пишется руками. Прежний список в тестах держал 58 маршрутов при 68
 * реальных, и расхождение ничем не ловилось: тест проходил, проверяя свой же
 * список.
 *
 * Почему генератор живёт в vitest, а не в скрипте на node: файлы маршрутов
 * импортируют компоненты, и вне vite-конвейера импорт `allRoutes` падает.
 *
 * Почему обновление файла вынесено из проверки: пока тест сначала писал файл,
 * а потом сравнивал его с содержимым, сравнение всегда проходило — файл к этому
 * моменту уже был равен ожидаемому. Расхождение с тем, что лежит в git, таким
 * тестом не обнаруживалось в принципе, то есть тест проверял сам себя. Теперь
 * проверка читает файл и сравнивает, ничего не записывая; обновление —
 * отдельная задача по <see cref="UPDATE_VARIABLE" />.
 */
const OUTPUT = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "routes.generated.json"
);

/**
 * Переменная, которой задаётся режим обновления файла.
 *
 * Обычный прогон тестов файл не трогает, поэтому забытый в коммите
 * routes.generated.json валит сборку, а не подменяется молча новым содержимым.
 * Обновить файл осознанно:
 *
 *   UPDATE_ROUTES_MANIFEST=1 npx vitest run src/tests/routesManifest.test.ts
 */
const UPDATE_VARIABLE = "UPDATE_ROUTES_MANIFEST";

/** Один маршрут в том виде, в каком его проверяет браузер. */
interface GeneratedRoute {
  path: string;
  type: string;
  name?: string;
}

function collect(): GeneratedRoute[] {
  return allRoutes.map(route => ({
    path: route.path,
    type: route.type,
    ...(route.name === undefined ? {} : { name: route.name }),
  }));
}

function serialize(routes: GeneratedRoute[]): string {
  return `${JSON.stringify({ routes }, null, 2)}\n`;
}

describe("генерация routes.generated.json", () => {
  it("описание маршрутов совпадает с кодом", () => {
    const expected = serialize(collect());

    if (process.env[UPDATE_VARIABLE] === "1") {
      writeFileSync(OUTPUT, expected, "utf8");

      return;
    }

    expect(
      existsSync(OUTPUT),
      `Нет файла ${OUTPUT}. Создайте его: ${UPDATE_VARIABLE}=1 npx vitest run src/tests/routesManifest.test.ts`
    ).toBe(true);

    const committed = readFileSync(OUTPUT, "utf8");

    expect(
      committed,
      `routes.generated.json разошёлся с allRoutes. Обновите его: ${UPDATE_VARIABLE}=1 npx vitest run src/tests/routesManifest.test.ts`
    ).toBe(expected);
  });

  it("маршрутов больше, чем в прежнем рукописном списке", () => {
    // Порог, при котором список перестаёт быть «примерно правильным».
    // При 68 маршрутах список из 58 означал бы, что десять экранов не проверяются.
    expect(collect().length).toBeGreaterThan(58);
  });

  it("пути уникальны", () => {
    const paths = collect().map(route => route.path);
    const duplicates = paths.filter(
      (path, index) => paths.indexOf(path) !== index
    );

    expect(duplicates).toEqual([]);
  });

  it("все пути начинаются со слэша", () => {
    const broken = collect()
      .map(route => route.path)
      .filter(path => !path.startsWith("/"));

    expect(broken).toEqual([]);
  });

  it("выходной файл существует и читается как json", () => {
    expect(existsSync(OUTPUT)).toBe(true);

    const parsed: unknown = JSON.parse(readFileSync(OUTPUT, "utf8"));

    expect(Array.isArray((parsed as { routes?: unknown }).routes)).toBe(true);
  });
});
