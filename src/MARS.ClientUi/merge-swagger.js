/**
 * merge-swagger.js
 * 
 * Объединяет несколько OpenAPI JSON файлов в один.
 * 
 * Использование: node merge-swagger.js api/*_api.json > api/swagger_api.json
 * Или: node merge-swagger.js file1.json file2.json ... > merged.json
 */

import fs from "fs";
import process from "process";

const files = process.argv.slice(2);

if (files.length === 0) {
  console.error("Использование: node merge-swagger.js file1.json file2.json ...");
  process.exit(1);
}

const mergedPaths = {};
const mergedSchemas = {};

for (const file of files) {
  try {
    const content = JSON.parse(fs.readFileSync(file, "utf-8"));
    
    for (const [path, methods] of Object.entries(content.paths || {})) {
      mergedPaths[path] = methods;
    }
    
    for (const [name, schema] of Object.entries(content.components?.schemas || {})) {
      if (!mergedSchemas[name]) {
        mergedSchemas[name] = schema;
      }
    }
  } catch (error) {
    console.error(`Ошибка чтения ${file}: ${error.message}`);
  }
}

const merged = {
  openapi: "3.0.1",
  info: {
    title: "MARS API — All Services",
    version: "1.0.0",
  },
  paths: mergedPaths,
  components: { schemas: mergedSchemas },
};

console.log(JSON.stringify(merged, null, 2));
