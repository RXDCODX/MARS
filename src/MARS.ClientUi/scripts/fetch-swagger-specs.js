/**
 * fetch-swagger-specs.js
 *
 * Собирает OpenAPI-спеки всех микросервисов через Gateway и сохраняет
 * объединённые документы в api/:
 *   - swagger_api.json  — REST-контракты (doc v1 каждого сервиса);
 *   - swagger_hubs.json — описание SignalR-хабов (doc hubs).
 *
 * Использование: node fetch-swagger-specs.js [gateway-url]
 * По умолчанию: http://localhost:6001
 */

import fs from "fs";
import { resolve } from "path";
import process from "process";

const GATEWAY_URL = (process.argv[2] || "http://localhost:6001").replace(
  /\/+$/,
  ""
);

// Порядок важен: при совпадении путей и схем побеждает первый сервис.
// Состав совпадает с ServiceEndpoints (Gateway), иначе часть контрактов
// молча выпадет из генерации.
const SERVICES = [
  "TwitchCore",
  "WaifuGacha",
  "Shikimori",
  "Telegram",
  "Discord",
  "Commands",
  "SoundRequest",
  "TTS",
  "OBS",
  "Alerts",
  "Scoreboard",
  "CinemaQueue",
  "MediaStorage",
  "Admin",
];

const API_DIR = resolve(process.cwd(), "./api");

async function fetchSpec(serviceName, document) {
  const url = `${GATEWAY_URL}/swagger/${serviceName}/${document}.json`;

  try {
    const response = await fetch(url);

    // hubs-документ есть не у каждого сервиса: 404 — нормальный ответ.
    if (!response.ok) {
      if (response.status !== 404) {
        console.warn(`⚠️ ${serviceName}/${document}: HTTP ${response.status}`);
      }
      return null;
    }

    const spec = await response.json();
    console.log(
      `✅ ${serviceName}/${document}: ${Object.keys(spec.paths || {}).length} paths`
    );
    return spec;
  } catch (error) {
    console.warn(`⚠️ ${serviceName}/${document}: ${error.message}`);
    return null;
  }
}

function mergeSpecs(specs) {
  const mergedPaths = {};
  const mergedSchemas = {};

  for (const [service, spec] of Object.entries(specs)) {
    for (const [path, methods] of Object.entries(spec.paths || {})) {
      if (mergedPaths[path]) {
        // Корень `/` объявлен у каждого сервиса как health-check. Оставляем
        // первый: клиенту нужен один метод, а не одиннадцать одинаковых.
        console.warn(`   ℹ️ ${path} уже объявлен, пропущен (${service})`);
        continue;
      }
      mergedPaths[path] = methods;
    }

    for (const [name, schema] of Object.entries(spec.components?.schemas || {})) {
      if (!mergedSchemas[name]) {
        mergedSchemas[name] = schema;
      }
    }
  }

  return {
    openapi: "3.0.1",
    info: {
      title: "MARS API — All Services",
      version: "1.0.0",
      description: "Merged API specification from all MARS microservices",
    },
    servers: [{ url: GATEWAY_URL }],
    paths: mergedPaths,
    components: { schemas: mergedSchemas },
  };
}

function writeMerged(specs, fileName) {
  const merged = mergeSpecs(specs);
  fs.writeFileSync(
    resolve(API_DIR, fileName),
    JSON.stringify(merged, null, 2)
  );

  console.log(
    `✅ api/${fileName}: ${Object.keys(merged.paths).length} путей, ` +
      `${Object.keys(merged.components.schemas).length} схем`
  );
}

async function collect(document) {
  const specs = {};
  let successCount = 0;

  for (const service of SERVICES) {
    const spec = await fetchSpec(service, document);
    if (spec) {
      specs[service] = spec;
      successCount++;
    }
  }

  console.log(`📊 ${document}: ${successCount}/${SERVICES.length} сервисов`);

  return { specs, successCount };
}

async function main() {
  console.log(`\n🔍 Собираем OpenAPI-спеки через Gateway: ${GATEWAY_URL}\n`);

  if (!fs.existsSync(API_DIR)) {
    fs.mkdirSync(API_DIR, { recursive: true });
  }

  const rest = await collect("swagger");
  if (rest.successCount === 0) {
    console.error("❌ Ни одна REST-спека не получена. Проверьте Gateway.");
    process.exit(1);
  }

  const hubs = await collect("hubs");
  if (hubs.successCount === 0) {
    console.error(
      "❌ Ни один документ хабов не получен: SignalR-клиенты не сгенерируются."
    );
    process.exit(1);
  }

  writeMerged(rest.specs, "swagger_api.json");
  writeMerged(hubs.specs, "swagger_hubs.json");

  console.log("\n✅ Спеки обновлены. Следующий шаг: yarn build:api");
}

main().catch(error => {
  console.error("❌ Ошибка:", error.message);
  process.exit(1);
});
