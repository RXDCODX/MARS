/**
 * fetch-swagger-specs.js
 * 
 * Собирает OpenAPI спеки со всех микросервисов через Gateway
 * и сохраняет в api/ директорию.
 * 
 * Использование: node fetch-swagger-specs.js [gateway-url]
 * По умолчанию: http://localhost:9155
 */

import fs from "fs";
import { resolve } from "path";
import process from "process";

const GATEWAY_URL = process.argv[2] || "http://localhost:9155";

const SERVICES = [
  "TwitchCore",
  "WaifuGacha", 
  "Telegram",
  "Commands",
  "SoundRequest",
  "OBS",
  "CinemaQueue",
  "MediaStorage",
  "Admin",
];

const API_DIR = resolve(process.cwd(), "./api");

async function fetchSwaggerSpec(serviceName) {
  const url = `${GATEWAY_URL}/swagger/${serviceName}/swagger.json`;
  
  try {
    const response = await fetch(url);
    if (!response.ok) {
      console.warn(`⚠️ ${serviceName}: HTTP ${response.status}`);
      return null;
    }
    const spec = await response.json();
    console.log(`✅ ${serviceName}: ${Object.keys(spec.paths || {}).length} paths`);
    return spec;
  } catch (error) {
    console.warn(`⚠️ ${serviceName}: ${error.message}`);
    return null;
  }
}

async function main() {
  console.log(`\n🔍 Собираем OpenAPI спеки через Gateway: ${GATEWAY_URL}\n`);

  if (!fs.existsSync(API_DIR)) {
    fs.mkdirSync(API_DIR, { recursive: true });
  }

  const results = {};
  let successCount = 0;

  for (const service of SERVICES) {
    const spec = await fetchSwaggerSpec(service);
    if (spec) {
      results[service] = spec;
      fs.writeFileSync(
        resolve(API_DIR, `${service.toLowerCase()}_api.json`),
        JSON.stringify(spec, null, 2)
      );
      successCount++;
    }
  }

  console.log(`\n📊 Результат: ${successCount}/${SERVICES.length} сервисов`);

  if (successCount === 0) {
    console.error("❌ Ни одна спека не получена. Проверьте Gateway.");
    process.exit(1);
  }

  // Объединяем все спеки в один файл
  const mergedPaths = {};
  const mergedSchemas = {};

  for (const [service, spec] of Object.entries(results)) {
    // Добавляем пути с префиксом сервиса для уникальности
    for (const [path, methods] of Object.entries(spec.paths || {})) {
      mergedPaths[path] = methods;
    }
    // Объединяем схемы (модели)
    for (const [name, schema] of Object.entries(spec.components?.schemas || {})) {
      if (!mergedSchemas[name]) {
        mergedSchemas[name] = schema;
      }
    }
  }

  const mergedSpec = {
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

  fs.writeFileSync(
    resolve(API_DIR, "swagger_api.json"),
    JSON.stringify(mergedSpec, null, 2)
  );

  console.log(`\n✅ Объединённая спека: api/swagger_api.json`);
  console.log(`   Путей: ${Object.keys(mergedPaths).length}`);
  console.log(`   Схем: ${Object.keys(mergedSchemas).length}`);
}

main().catch(error => {
  console.error("❌ Ошибка:", error.message);
  process.exit(1);
});
