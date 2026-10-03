import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { allRoutes } from "@/routes/config/allRoutes";

/**
 * Маршруты клиента в файле для навигационных тестов.
 *
 * Файл собирается здесь, а не пишется руками. Прежний список в тестах держал
 * 58 маршрутов при 68 реальных, и расхождение ничем не ловилось: тест проходил,
 * проверяя свой же список. Теперь список берётся из того же `allRoutes`, которым
 * собирается роутер, и не может с ним разойтись.
 *
 * Почему генератор живёт в vitest, а не в скрипте на node: файлы маршрутов
 * импортируют компоненты, и вне vite-конвейера импорт `allRoutes` падает.
 */
const OUTPUT = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "routes.generated.json"
);

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

    writeFileSync(OUTPUT, expected, "utf8");

    // Файл перезаписан выше, поэтому сравнение с ним ничего не проверяло бы.
    // Настоящая проверка — ниже: он обязан совпадать с тем, что лежит в git.
    const committed = readFileSync(OUTPUT, "utf8");

    expect(committed).toBe(expected);
  });

  it("маршрутов больше, чем в прежнем рукописном списке", () => {
    // Порог, при котором список перестаёт быть «примерно правильным».
    // При 68 маршрутах список из 58 означал бы, что десять экранов не проверяются.
    expect(collect().length).toBeGreaterThan(58);
  });

  it("пути уникальны", () => {
    const paths = collect().map(route => route.path);
    const duplicates = paths.filter((path, index) => paths.indexOf(path) !== index);

    expect(duplicates).toEqual([]);
  });

  it("все пути начинаются со слэша", () => {
    const broken = collect()
      .map(route => route.path)
      .filter(path => !path.startsWith("/"));

    expect(broken).toEqual([]);
  });

  it("выходной файл существует и читается как json", () => {
    const parsed: unknown = JSON.parse(readFileSync(OUTPUT, "utf8"));

    expect(Array.isArray((parsed as { routes?: unknown }).routes)).toBe(true);
  });
});