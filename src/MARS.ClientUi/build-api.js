import { exec } from "child_process";
import fs from "fs";
import { resolve } from "path";
import process from "process";
import { generateApi } from "swagger-typescript-api";

// Функция для автоматического определения контроллеров из swagger (api)
function getControllersFromSwagger(swaggerSource) {
  const controllers = new Set();

  Object.keys(swaggerSource.paths).forEach(path => {
    const pathData = swaggerSource.paths[path];

    // Проверяем все методы в пути
    Object.values(pathData).forEach(methodData => {
      if (methodData.tags && Array.isArray(methodData.tags)) {
        methodData.tags.forEach(tag => {
          if (
            tag &&
            !tag.toLowerCase().includes("hub") &&
            // Служебные страницы и health-check попадают в документ с тегом
            // вида "MARS.MediaStorage, Version=1.0.0.0, ...": из такого имени
            // получается клиент с пробелами и запятыми в идентификаторе.
            !tag.includes("Version=") &&
            path !== "/" &&
            path !== "/storage-ui"
          ) {
            // Добавляем тег как контроллер, если это не хаб
            controllers.add(tag);
          }
        });
      }
    });
  });

  return Array.from(controllers);
}

// Функция для автоматического определения SignalR хабов из swagger (hubs)
function getSignalRHubsFromSwagger(swaggerSource) {
  const hubPaths = new Map(); // Map для хранения пути к хабу

  // Ищем в путях
  Object.keys(swaggerSource.paths).forEach(path => {
    const pathData = swaggerSource.paths[path];

    // Проверяем все методы в пути
    Object.values(pathData).forEach(methodData => {
      if (methodData.tags && Array.isArray(methodData.tags)) {
        methodData.tags.forEach(tag => {
          if (tag && tag.toLowerCase().includes("hub")) {
            // Извлекаем базовый путь к хабу
            let hubPath;

            if (path.startsWith("/hubs/")) {
              // Для путей вида /hubs/HubName/...
              const hubName = path.split("/")[2];
              hubPath = `/hubs/${hubName}`;
            } else if (path.startsWith("/") && !path.startsWith("/api/")) {
              // Для путей вида /hubname/...
              const hubName = path.split("/")[1];
              hubPath = `/${hubName}`;
            }

            if (hubPath) {
              hubPaths.set(tag, hubPath);
            }
          }
        });
      }
    });
  });

  return Array.from(hubPaths.entries()).map(([hubName, hubPath]) => ({
    name: hubName,
    path: hubPath,
  }));
}

// Определяем контроллеры и хабы динамически
let CONTROLLERS = [];
let SIGNALR_HUBS = [];

// Новая структура папок: все типы хранятся в одной папке types
const OUTPUT_DIR_ROOT = resolve(process.cwd(), "./src/shared/api/");
const OUTPUT_DIR_TYPES = resolve(OUTPUT_DIR_ROOT, "./types/"); // Все общие типы: data-contracts.ts и signalr-types.ts
const OUTPUT_DIR_HTTP_CLIENTS = resolve(OUTPUT_DIR_ROOT, "./http-clients/"); // HTTP клиенты
const OUTPUT_DIR_SIGNALR_CLIENTS = resolve(
  OUTPUT_DIR_ROOT,
  "./signalr-clients/"
); // SignalR клиенты

// Функция для очистки папки API
function cleanApiDirectory() {
  console.log("🧹 Очищаем только сгенерированные файлы API...");

  // Не удаляем всю shared/api: рядом с генерацией лежат поддерживаемые вручную
  // тесты и исходники шаблонов. Генерируемые каталоги можно пересоздавать.
  const generatedDirectories = [
    OUTPUT_DIR_TYPES,
    OUTPUT_DIR_HTTP_CLIENTS,
    OUTPUT_DIR_SIGNALR_CLIENTS,
    resolve(OUTPUT_DIR_ROOT, ".temp"),
  ];

  generatedDirectories.forEach(directory => {
    if (fs.existsSync(directory)) {
      fs.rmSync(directory, { recursive: true, force: true });
    }
  });

  const generatedRootFiles = [
    resolve(OUTPUT_DIR_ROOT, "api-config.ts"),
    resolve(OUTPUT_DIR_ROOT, "index.ts"),
  ];

  generatedRootFiles.forEach(file => {
    if (fs.existsSync(file)) {
      fs.rmSync(file, { force: true });
    }
  });

  [OUTPUT_DIR_ROOT, ...generatedDirectories.slice(0, 3)].forEach(directory => {
    fs.mkdirSync(directory, { recursive: true });
  });

  console.log("✅ Каталоги генерации подготовлены");
}

// Функция для дедупликации одинаковых перечислений в сгенерированном файле
function deduplicateEnums(filePath) {
  let content = fs.readFileSync(filePath, "utf-8");

  // swagger-typescript-api с extractEnums пишет `export enum`, а для
  // переиспользованных inline-перечислений — ещё и `export type X = Y;`.
  // Обе формы собираем вместе: дедуплицировать надо именно тела, а форма
  // объявления у дубликатов бывает разной.
  // Тело type-алиаса намеренно не пропускает фигурные скобки: так под
  // дедупликацию попадают только простые алиасы и объединения литералов, а
  // объектные типы с `;` внутри не разрезаются регуляркой на половины.
  const declarationPattern =
    /export (enum|type) (\w+)(?:<[^>]*>)? (?:\{([\s\S]*?)\n\}|=([^{}]*?);)/g;
  const declarations = [];
  let match;

  while ((match = declarationPattern.exec(content)) !== null) {
    const [, kind, name, enumBody, typeBody] = match;
    declarations.push({
      kind,
      name,
      value: match[0],
      // Тело без имени: два перечисления с одинаковым набором членов и
      // разными именами — один и тот же тип.
      signature: (enumBody ?? typeBody ?? "")
        .replace(/\s+/g, " ")
        .trim()
        .replace(new RegExp(`\\b${name}\\b`, "g"), "PLACEHOLDER"),
    });
  }

  const enumGroups = new Map();
  declarations.forEach(declaration => {
    if (!declaration.signature) {
      return;
    }

    if (!enumGroups.has(declaration.signature)) {
      enumGroups.set(declaration.signature, []);
    }
    enumGroups.get(declaration.signature).push(declaration);
  });

  enumGroups.forEach(group => {
    if (group.length < 2) {
      return;
    }

    const base = group[0];

    group.slice(1).forEach(duplicate => {
      // Перечисление живёт и в типовой, и в значенийной позиции. Одного
      // `type` мало: `Duplicate.Value` перестал бы компилироваться, а одного
      // `const` мало для аннотаций. Поэтому дубликат становится парой
      // type + const над базовым объявлением.
      const replacement =
        duplicate.kind === "enum"
          ? `export type ${duplicate.name} = ${base.name};\nexport const ${duplicate.name} = ${base.name};`
          : `export type ${duplicate.name} = ${base.name};`;

      content = content.replace(duplicate.value, replacement);
    });
  });

  fs.writeFileSync(filePath, content);
  console.log(`✅ Дедупликация enum'ов выполнена для ${filePath}`);

  // Возвращаем группы enum'ов для создания общего файла
  return enumGroups;
}

// Разбирает объявления *OperationResult и возвращает карту
// «имя → обобщённый тип». База берётся из поля result самого объявления: в
// имени она теряется (AutoMessageDtoArray — это AutoMessageDto[]), а в теле
// ещё и бывает примитивом C# (Int32 — это number).
function collectOperationResultReplacements(content) {
  const replacements = new Map();
  const startPattern = /export (interface|type) (\w*OperationResult)\b/;
  const startRe = new RegExp(startPattern.source, "g");
  let match;

  while ((match = startRe.exec(content)) !== null) {
    const start = match.index;
    const kind = match[1];
    const name = match[2];
    let end = start;

    if (kind === "interface") {
      const braceStart = content.indexOf("{", start);
      let depth = 0;
      for (let i = braceStart; i < content.length; i++) {
        if (content[i] === "{") {
          depth++;
        } else if (content[i] === "}") {
          depth--;
          if (depth === 0) {
            end = i + 1;
            break;
          }
        }
      }
    } else {
      let depth = 0;
      for (let i = start; i < content.length; i++) {
        if (content[i] === "{") {
          depth++;
        } else if (content[i] === "}") {
          depth--;
        } else if (content[i] === ";" && depth === 0) {
          end = i + 1;
          break;
        }
      }
    }

    const declaration = content.slice(start, end);
    const resultMatch = declaration.match(/\bresult\??:\s*([^,;\n}]+)/);

    if (name !== "OperationResult" && resultMatch) {
      let base = resultMatch[1].trim();
      // Сгенерированное `(AutoMessageDto)[]` приводим к `AutoMessageDto[]`.
      base = base.replace(/^\((.*)\)(\[\])?$/, "$1$2");
      replacements.set(name, `OperationResult<${base}>`);
    }

    startRe.lastIndex = end;
  }

  return replacements;
}

// Собирает диапазоны объявлений, чьё имя оканчивается на OperationResult.
function collectOperationResultRanges(content) {
  const ranges = [];
  const pattern = /export (interface|type) (\w*OperationResult)\b/g;
  let match;

  while ((match = pattern.exec(content)) !== null) {
    const start = match.index;
    const kind = match[1];
    let end = start;

    if (kind === "interface") {
      const braceStart = content.indexOf("{", start);
      let depth = 0;
      for (let i = braceStart; i < content.length; i++) {
        if (content[i] === "{") {
          depth++;
        } else if (content[i] === "}") {
          depth--;
          if (depth === 0) {
            end = i + 1;
            break;
          }
        }
      }
    } else {
      let depth = 0;
      for (let i = start; i < content.length; i++) {
        if (content[i] === "{") {
          depth++;
        } else if (content[i] === "}") {
          depth--;
        } else if (content[i] === ";" && depth === 0) {
          end = i + 1;
          break;
        }
      }
    }

    ranges.push({ start, end });
    pattern.lastIndex = end;
  }

  return ranges;
}

// Функция для замены дублирующихся OperationResult типов на обобщенный
function replaceOperationResultTypes(filePath) {
  let content = fs.readFileSync(filePath, "utf-8");

  console.log(`🔧 Обрабатываем OperationResult типы в ${filePath}...`);

  const replacements = collectOperationResultReplacements(content);
  const ranges = collectOperationResultRanges(content);

  console.log(
    `   Найдено ${ranges.length} объявлений OperationResult, ` +
      `${replacements.size} конкретных конвертов`
  );

  // Сначала удаляем объявления (с конца, чтобы не сдвигать индексы), затем
  // заменяем их использования на OperationResult<T>.
  for (const { start, end } of ranges.slice().reverse()) {
    // Захватываем один перевод строки, чтобы не оставлять лесенку пустых.
    const tail = content[end] === "\n" ? end + 1 : end;
    content = content.slice(0, start) + content.slice(tail);
  }

  for (const [name, replacement] of replacements) {
    content = content.replace(new RegExp(`\\b${name}\\b`, "g"), replacement);
  }

  const genericOperationResult = `/**
 * Обобщённый результат операции.
 *
 * Поля data и result повторяют друг друга: транспорт кладёт полезную нагрузку
 * в оба поля, потому что вызовы на проекте читают response.data.data, а
 * сервис отвечает result.
 * @template TData - Тип данных, возвращаемых в поле data
 */
export interface OperationResult<TData = any> {
    /** Флаг успешности операции */
    success: boolean;
    /** Сообщение о результате операции */
    message?: string;
    /** Сообщение об ошибке */
    errorMessage?: string;
    /** Данные результата операции */
    data?: TData;
    /** Данные результата операции в форме сервиса */
    result?: TData;
}
`;

  const insertPosition = content.search(/export (interface|type|class|enum)/);
  if (insertPosition === -1) {
    throw new Error("В сгенерированном data-contracts нет ни одного экспорта");
  }

  content =
    content.slice(0, insertPosition) +
    genericOperationResult +
    "\n" +
    content.slice(insertPosition);

  // Убираем избыточные пустые строки
  content = content.replace(/\n{3,}/g, "\n\n");

  fs.writeFileSync(filePath, content);
  console.log("✅ OperationResult типы успешно обработаны");

  return {
    processedTypes: ranges.length,
    replacements: replacements.size,
  };
}

// Функция для генерации data-contracts отдельно (внутри папки types)
async function generateDataContracts() {
  console.log("📋 Генерируем data-contracts...");

  // Копируем data-contracts из http-clients в types
  const httpClientsDataContractsPath = resolve(
    OUTPUT_DIR_HTTP_CLIENTS,
    "data-contracts.ts"
  );
  const typesDataContractsPath = resolve(OUTPUT_DIR_TYPES, "data-contracts.ts");

  if (fs.existsSync(httpClientsDataContractsPath)) {
    // Гарантируем, что папка types существует
    if (!fs.existsSync(OUTPUT_DIR_TYPES)) {
      fs.mkdirSync(OUTPUT_DIR_TYPES, { recursive: true });
    }
    fs.copyFileSync(httpClientsDataContractsPath, typesDataContractsPath);
    console.log("✅ Data-contracts скопированы в types/data-contracts.ts");

    // Заменяем дублирующиеся OperationResult типы на обобщенный
    replaceOperationResultTypes(typesDataContractsPath);

    // Удаляем временный файл из http-clients (он нам больше не нужен)
    fs.unlinkSync(httpClientsDataContractsPath);
    console.log("   🗑️ Временный data-contracts.ts удален из http-clients");
  } else {
    console.log(
      "⚠️ Файл http-clients/data-contracts.ts не найден, пропускаем генерацию data-contracts"
    );
  }
}

// Готовит локальную копию спецификации для генератора клиентов.
//
// swagger-typescript-api создаёт модуль на каждый тег, включая служебные:
// страницы "/" и "/storage-ui" помечены именем сборки с версией. Из такого
// тега получается клиент с пробелами, запятыми и точкой в идентификаторе, а
// методы ведут на страницы, которых в UI нет. Снимок api/swagger_api.json
// остаётся как есть — чистим только копию, которую читает генератор.
function prepareSwaggerSpecForGeneration() {
  const sourcePath = resolve(process.cwd(), "./api/swagger_api.json");
  const spec = JSON.parse(fs.readFileSync(sourcePath, "utf-8"));
  const excludedPaths = ["/", "/storage-ui"];

  excludedPaths.forEach(path => {
    delete spec.paths[path];
  });

  const tempDir = resolve(OUTPUT_DIR_ROOT, ".temp");
  fs.mkdirSync(tempDir, { recursive: true });

  const outputPath = resolve(tempDir, "swagger_api.generated.json");
  fs.writeFileSync(outputPath, JSON.stringify(spec, null, 2));

  return outputPath;
}

// Функция для генерации HTTP клиентов (без типов)
async function generateHttpClients(swaggerApiJsonPath) {
  console.log("🌐 Генерируем HTTP клиенты...");

  await generateApi({
    input: swaggerApiJsonPath,
    output: OUTPUT_DIR_HTTP_CLIENTS,
    name: "types.ts",
    cleanOutput: true,
    httpClientType: "axios",
    prettier: {
      trailingComma: "all",
      tabWidth: 4,
      printWidth: 160,
    },
    generateClient: true, // Только клиенты
    modular: true, // Модульная структура
    moduleNameFirstTag: true,
    sortTypes: false, // Не нужно для клиентов
    enumNamesAsValues: false,
    generateResponses: true,
    extractEnums: true, // Извлекаем enum'ы для клиентов
    codeGenConstructs: constructs => ({
      ...constructs,
      NullValue: () => "undefined",
      TypeField: ({ readonly, key, value }) => {
        const hadNull = typeof value === "string" && /\|\s*null\b/.test(value);
        const hadUndefined =
          typeof value === "string" && /\|\s*undefined\b/.test(value);
        let cleaned = String(value)
          .replace(/\s*\|\s*null\b/g, "")
          .replace(/\s*\|\s*undefined\b/g, "")
          .trim();
        const optionalMark = hadNull || hadUndefined ? "?" : "";
        return `${readonly ? "readonly " : ""}${key}${optionalMark}: ${cleaned}`;
      },
      // Используем TypeWithGeneric для корректной генерации Generic типов
      TypeWithGeneric: (typeName, genericArgs) =>
        `${typeName}<${Array.isArray(genericArgs) ? genericArgs.join(", ") : genericArgs}>`,
    }),
  });

  // swagger-typescript-api генерирует собственный http-client.ts. Заменяем его
  // поддерживаемой версией с обработкой OperationResult и SPA fallback.
  const httpClientTemplatePath = resolve(
    process.cwd(),
    "./scripts/templates/http-client.ts"
  );
  if (!fs.existsSync(httpClientTemplatePath)) {
    throw new Error("Не найден шаблон HTTP-транспорта: " + httpClientTemplatePath);
  }
  fs.copyFileSync(
    httpClientTemplatePath,
    resolve(OUTPUT_DIR_HTTP_CLIENTS, "http-client.ts")
  );

  // Клиенты возвращают именованные конверты (AutoMessageDtoArrayOperationResult
  // и подобные). Объявления этих типов останутся в data-contracts, но затем
  // заменятся на OperationResult<T>, поэтому сигнатуры клиентов надо править
  // той же картой по сгенерированному файлу типов.
  const candidateTypesFiles = ["types.ts", "data-contracts.ts"];
  const generatedTypesFile = candidateTypesFiles.find(name =>
    fs.existsSync(resolve(OUTPUT_DIR_HTTP_CLIENTS, name))
  );
  const generatedTypesPath = generatedTypesFile
    ? resolve(OUTPUT_DIR_HTTP_CLIENTS, generatedTypesFile)
    : null;
  const operationResultReplacements = generatedTypesPath
    ? collectOperationResultReplacements(
        fs.readFileSync(generatedTypesPath, "utf-8")
      )
    : new Map();

  // Исправляем импорты в каждом клиенте на общий источник типов и удаляем локальные data-contracts
  const clientFiles = fs
    .readdirSync(OUTPUT_DIR_HTTP_CLIENTS)
    .filter(file => file.endsWith(".ts"));

  console.log(
    `\n🔧 Обрабатываем ${clientFiles.length} файлов HTTP клиентов...`
  );

  clientFiles.forEach(file => {
    const filePath = resolve(OUTPUT_DIR_HTTP_CLIENTS, file);

    // Файл типов обрабатывается отдельно: он должен остаться под
    // каноническим именем data-contracts.ts для последующего переноса.
    if (file === "types.ts" || file === "data-contracts.ts") {
      return;
    }

    let content = fs.readFileSync(filePath, "utf-8");

    // Заменяем импорты локальных контрактов на общий файл типов
    const importsBefore = (content.match(/import.*from.*data-contracts/g) || [])
      .length;

    // Функция для парсинга импортов с учетом Generic типов
    const parseImports = importString => {
      const baseTypes = new Set();
      const enumTypes = new Set();
      const paramsTypes = new Set();

      // Разбиваем по запятым, учитывая вложенные <>
      let currentToken = "";
      let bracketDepth = 0;

      for (let char of importString + ",") {
        if (char === "<") {
          bracketDepth++;
        } else if (char === ">") {
          bracketDepth--;
        } else if (char === "," && bracketDepth === 0) {
          const trimmed = currentToken.trim();
          if (trimmed) {
            // Если это OperationResult<ConcreteType>, извлекаем ConcreteType
            const genericMatch = trimmed.match(/^OperationResult<(.+)>$/);
            if (genericMatch) {
              const innerType = genericMatch[1];
              // Убираем [] для массивов и добавляем базовый тип
              const cleanType = innerType.replace(/\[\]$/, "");
              if (
                !cleanType.match(
                  /^(string|number|boolean|object|any|Int32|String|Object)$/i
                )
              ) {
                baseTypes.add(cleanType);
              }
            }
            // Проверяем на формат без угловых скобок: OperationResultTypeName или OperationResultTypeName[]
            else if (trimmed.match(/^OperationResult[A-Z]/)) {
              // Извлекаем тип: OperationResultApiMediaInfo[] -> ApiMediaInfo
              const typeMatch = trimmed.match(
                /^OperationResult([A-Z][a-zA-Z0-9]*)/
              );
              if (typeMatch) {
                const extractedType = typeMatch[1];
                if (!extractedType.match(/^(Int32|String|Object)$/)) {
                  baseTypes.add(extractedType);
                }
              }
            } else if (trimmed.includes("Params") && trimmed.endsWith("Enum")) {
              // Это enum параметров, сохраняем отдельно
              paramsTypes.add(trimmed);
            } else if (trimmed.endsWith("Enum")) {
              // Это обычный enum, сохраняем
              enumTypes.add(trimmed);
            } else if (trimmed.endsWith("OperationResult")) {
              // Пропускаем конкретные OperationResult типы - они будут заменены на generic
              // Не добавляем их в импорты
            } else if (trimmed !== "OperationResult") {
              // Обычный тип, сохраняем
              baseTypes.add(trimmed);
            }
          }
          currentToken = "";
        } else {
          currentToken += char;
        }
      }

      return { baseTypes, enumTypes, paramsTypes };
    };

    // Обрабатываем все импорты (могут быть многострочными)
    content = content.replace(
      /import\s+(?:type\s*)?\{([^}]+)\}\s+from\s+["']\.\/data-contracts["']/gs,
      (_m, p1) => {
        const { baseTypes, enumTypes, paramsTypes } = parseImports(p1);

        // Всегда добавляем базовый OperationResult
        baseTypes.add("OperationResult");

        // Объединяем все типы в правильном порядке
        const allTypes = [
          ...Array.from(baseTypes).sort(),
          ...Array.from(enumTypes).sort(),
          ...Array.from(paramsTypes).sort(),
        ];

        const cleanedImports = allTypes.join(", ");

        return `import type { ${cleanedImports} } from "../types/data-contracts";`;
      }
    );

    // Заменяем использования конкретных типов OperationResult на обобщённый
    // в сигнатурах методов и типах возврата.
    for (const [name, replacement] of operationResultReplacements) {
      content = content.replace(new RegExp(`\\b${name}\\b`, "g"), replacement);
    }

    // Убираем лишние пустые строки и запятые в импортах
    content = content.replace(/\n\s*\n\s*\n/g, "\n\n");
    content = content.replace(/,\s*,/g, ","); // Двойные запятые
    content = content.replace(/,\s*\}/g, "}"); // Запятая перед закрывающей скобкой

    fs.writeFileSync(filePath, content);

    if (importsBefore > 0) {
      console.log(`   ✓ ${file}: очищено импортов`);
    }
  });

  if (generatedTypesFile && generatedTypesFile !== "data-contracts.ts") {
    fs.renameSync(
      resolve(OUTPUT_DIR_HTTP_CLIENTS, generatedTypesFile),
      resolve(OUTPUT_DIR_HTTP_CLIENTS, "data-contracts.ts")
    );
    console.log(
      `   📋 Переименован ${generatedTypesFile} в data-contracts.ts для обратной совместимости`
    );
  }

  console.log("\n✅ HTTP клиенты сгенерированы и обработаны в http-clients/");
  console.log("   ✓ Импорты переведены на общий источник типов");
  console.log("   ✓ Приводимые типы OperationResult заменены на OperationResult<T>");
}

// Функция для извлечения всех экспортируемых типов из файла
function extractExportedTypes(filePath) {
  if (!fs.existsSync(filePath)) {
    return new Set();
  }

  const content = fs.readFileSync(filePath, "utf-8");
  const exportedTypes = new Set();

  // Паттерны для различных экспортов
  const patterns = [
    /export\s+interface\s+(\w+)/g,
    /export\s+type\s+(\w+)\s*=/g,
    /export\s+enum\s+(\w+)/g,
    /export\s+class\s+(\w+)/g,
  ];

  patterns.forEach(pattern => {
    let match;
    while ((match = pattern.exec(content)) !== null) {
      exportedTypes.add(match[1]);
    }
  });

  return exportedTypes;
}

// Функция для извлечения определений типов из файла (полное определение, не только имя)
function extractTypeDefinitions(filePath) {
  if (!fs.existsSync(filePath)) {
    return [];
  }

  const content = fs.readFileSync(filePath, "utf-8");
  const definitions = [];

  // Паттерны для различных экспортов с полным определением
  const patterns = [
    {
      // interface с возможным JSDoc
      regex: /(?:\/\*\*[\s\S]*?\*\/\s*)?export\s+interface\s+(\w+)[\s\S]*?\n}/g,
      type: "interface",
    },
    {
      // type с возможным JSDoc
      regex: /(?:\/\*\*[\s\S]*?\*\/\s*)?export\s+type\s+(\w+)\s*=[\s\S]*?;/g,
      type: "type",
    },
    {
      // enum с возможным JSDoc
      regex: /(?:\/\*\*[\s\S]*?\*\/\s*)?export\s+enum\s+(\w+)\s*\{[\s\S]*?\n}/g,
      type: "enum",
    },
  ];

  patterns.forEach(({ regex, type }) => {
    let match;
    const contentCopy = content;
    regex.lastIndex = 0;
    while ((match = regex.exec(contentCopy)) !== null) {
      definitions.push({
        name: match[1],
        type: type,
        fullDefinition: match[0],
      });
    }
  });

  return definitions;
}

// Функция для объединения SignalR типов в data-contracts
async function mergeSignalRTypesIntoDataContracts() {
  console.log("📡 Добавляем уникальные SignalR типы в data-contracts...");

  const swaggerHubsJsonPath = resolve(process.cwd(), "./api/swagger_hubs.json");
  const tempDir = resolve(OUTPUT_DIR_ROOT, ".temp");

  // Создаем временную директорию
  if (!fs.existsSync(tempDir)) {
    fs.mkdirSync(tempDir, { recursive: true });
  }

  // Генерируем SignalR типы во временную папку
  await generateApi({
    input: swaggerHubsJsonPath,
    output: tempDir,
    name: "signalr-temp.ts",
    cleanOutput: true,
    httpClientType: "axios",
    prettier: {
      trailingComma: "all",
      tabWidth: 4,
      printWidth: 160,
    },
    generateClient: false, // Только типы
    modular: false, // Один файл
    sortTypes: true,
    extractEnums: true,
    codeGenConstructs: constructs => ({
      ...constructs,
      NullValue: () => "undefined",
      TypeField: ({ readonly, key, value }) => {
        const hadNull = typeof value === "string" && /\|\s*null\b/.test(value);
        const hadUndefined =
          typeof value === "string" && /\|\s*undefined\b/.test(value);
        let cleaned = String(value)
          .replace(/\s*\|\s*null\b/g, "")
          .replace(/\s*\|\s*undefined\b/g, "")
          .trim();
        const optionalMark = hadNull || hadUndefined ? "?" : "";
        return `${readonly ? "readonly " : ""}${key}${optionalMark}: ${cleaned}`;
      },
    }),
  });

  // Переименовываем файл если он создался с неправильным именем
  const apiFilePath = resolve(tempDir, "Api.ts");
  const tempSignalrFilePath = resolve(tempDir, "signalr-temp.ts");
  if (fs.existsSync(apiFilePath)) {
    fs.renameSync(apiFilePath, tempSignalrFilePath);
  }

  const dataContractsPath = resolve(OUTPUT_DIR_TYPES, "data-contracts.ts");

  // Извлекаем типы из обоих файлов
  const dataContractsTypes = extractExportedTypes(dataContractsPath);
  const signalrDefinitions = extractTypeDefinitions(tempSignalrFilePath);

  // Находим уникальные типы, которых нет в data-contracts
  const uniqueSignalRDefinitions = signalrDefinitions.filter(
    def => !dataContractsTypes.has(def.name)
  );

  const duplicateCount =
    signalrDefinitions.length - uniqueSignalRDefinitions.length;

  console.log(
    `   Найдено ${signalrDefinitions.length} SignalR типов, из них ${duplicateCount} дубликатов`
  );

  if (uniqueSignalRDefinitions.length > 0) {
    console.log(
      `   Добавляем ${uniqueSignalRDefinitions.length} уникальных SignalR типов в data-contracts.ts:`
    );
    console.log(
      `   ${uniqueSignalRDefinitions.map(def => def.name).join(", ")}`
    );

    // Читаем data-contracts
    let dataContractsContent = fs.readFileSync(dataContractsPath, "utf-8");

    // Добавляем уникальные типы в конец файла
    const signalRSection = `\n// ========================================\n// SignalR-специфичные типы\n// ========================================\n\n${uniqueSignalRDefinitions.map(def => def.fullDefinition).join("\n\n")}\n`;

    dataContractsContent = dataContractsContent.trimEnd() + signalRSection;

    // Сохраняем и применяем дедупликацию enum'ов
    fs.writeFileSync(dataContractsPath, dataContractsContent);
    deduplicateEnums(dataContractsPath);

    console.log("   ✓ Уникальные SignalR типы добавлены в data-contracts.ts");
  } else {
    console.log("   ✓ Все SignalR типы уже присутствуют в data-contracts.ts");
  }

  // Удаляем временную директорию
  fs.rmSync(tempDir, { recursive: true, force: true });
  console.log("   🗑️ Временные файлы удалены");

  console.log("✅ Все типы объединены в types/data-contracts.ts");
}

// Функция для создания утилит конфигурации API
function createApiConfig() {
  const apiConfigContent = `/**
 * Утилиты для конфигурации API клиентов
 */

/**
 * Получает базовый URL для API запросов в рантайме
 * Использует VITE_API_BASE_URL; VITE_BASE_PATH остаётся fallback для старых конфигураций
 */
export function getApiBaseUrl(): string {
  // В браузере используем import.meta.env
  if (typeof window !== "undefined" && typeof import.meta !== "undefined") {
    const basePath =
      import.meta.env.VITE_API_BASE_URL || import.meta.env.VITE_BASE_PATH;
    if (basePath) {
      return basePath.replace(/\\/$/, "");
    }
  }

  // В Node.js используем process.env
  if (typeof process !== "undefined" && process.env) {
    const basePath =
      process.env.VITE_API_BASE_URL || process.env.VITE_BASE_PATH;
    if (basePath) {
      return basePath.replace(/\\/$/, "");
    }
  }

  // По умолчанию возвращаем пустую строку
  return "";
}

/**
 * Создает полный URL для API endpoint
 * @param endpoint - путь к endpoint (например, "/api/commands")
 * @returns полный URL
 */
export function createApiUrl(endpoint: string): string {
  const baseUrl = getApiBaseUrl();
  const cleanEndpoint = endpoint.startsWith("/") ? endpoint : \`/\${endpoint}\`;

  return \`\${baseUrl}\${cleanEndpoint}\`;
}

/**
 * Конфигурация по умолчанию для API клиентов
 */
export const defaultApiConfig = {
  baseURL: getApiBaseUrl(),
};
`;

  fs.writeFileSync(resolve(OUTPUT_DIR_ROOT, "api-config.ts"), apiConfigContent);
  console.log("✅ API конфигурация создана в api-config.ts");
}

// Функция для генерации SignalR клиентов
async function generateSignalRClients() {
  console.log(
    "📋 Используется шаблон React wrapper: react-signalr с children props"
  );

  for (const hub of SIGNALR_HUBS) {
    // Создаем папку для каждого хаба
    const hubDir = resolve(OUTPUT_DIR_SIGNALR_CLIENTS, `${hub.name}`);
    if (!fs.existsSync(hubDir)) {
      fs.mkdirSync(hubDir, { recursive: true });
    }

    // Используем путь к хабу напрямую из swagger.json
    const hubUrl = hub.path.startsWith("/") ? hub.path.slice(1) : hub.path;

    // Первый файл - SignalR контекст
    const signalRContextContent = `import { HubConnectionBuilder, IRetryPolicy } from "@microsoft/signalr";

import { logger } from "@/shared/logger";

const policy: IRetryPolicy = { nextRetryDelayInMilliseconds: () => 5000 };

const baseUrl = import.meta.env.VITE_BASE_PATH;

export const ${hub.name}SignalRConnectionBuilder = new HubConnectionBuilder()
  .withUrl(baseUrl + "${hubUrl}")
  .withAutomaticReconnect(policy)
  .configureLogging(logger);
`;

    fs.writeFileSync(
      resolve(hubDir, "SignalRContext.ts"),
      signalRContextContent
    );

    // Второй файл - React wrapper
    const reactWrapperContent = `import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

export const ${hub.name}SignalRContext = signalR.createSignalRContext({});

interface ${hub.name}Properties {
  children: React.ReactNode;
}

export function ${hub.name}SignalRHubWrapper({
  children,
}: ${hub.name}Properties) {
  return (
    <${hub.name}SignalRContext.Provider
      automaticReconnect={true}
      onError={async error => { console.log(error); }}
      onClosed={async event => { console.log(event); }}
      onOpen={async event => { console.log(event); }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "${hubUrl}"}
      logMessageContent
    >
      {children}
    </${hub.name}SignalRContext.Provider>
  );
}
`;

    fs.writeFileSync(
      resolve(hubDir, "SignalRHubWrapper.tsx"),
      reactWrapperContent
    );

    console.log(
      `✅ SignalR клиент для ${hub.name} создан в signalr-clients/${hub.name}/`
    );
  }
}

// Функция для создания индексного файла
function createIndexFile() {
  // Используем реально сгенерированные файлы: генератор нормализует имена
  // из тегов (например, NSFWBooruAutoPost -> NsfwBooruAutoPost), поэтому
  // экспортировать по сырым тегам нельзя - получим MISSING_EXPORT
  const httpClientNames = fs
    .readdirSync(OUTPUT_DIR_HTTP_CLIENTS)
    .filter(file => file.endsWith(".ts"))
    .map(file => file.replace(/\.ts$/, ""))
    .filter(name => !["http-client", "data-contracts", "types"].includes(name));

  const indexContent = `// Автоматически сгенерированный индексный файл
// Импорты утилит конфигурации
export * from "./api-config";

// Импорты типов (все типы из одного источника)
export * from "./types/data-contracts";

// Типы, которых нет в OpenAPI: нагрузки хаба, приезжающие полями bytes,
// описываются вручную — генератор видит их как number[].
export * from "./manual";

// Импорты HTTP клиентов
${httpClientNames.map(name => `export { ${name} } from "./http-clients/${name}";`).join("\n")}

// Импорты SignalR клиентов
${SIGNALR_HUBS.map(hub => `export { ${hub.name}SignalRConnectionBuilder } from "./signalr-clients/${hub.name}/SignalRContext";`).join("\n")}
${SIGNALR_HUBS.map(hub => `export { ${hub.name}SignalRContext } from "./signalr-clients/${hub.name}/SignalRHubWrapper";`).join("\n")}
${SIGNALR_HUBS.map(hub => `export { ${hub.name}SignalRHubWrapper } from "./signalr-clients/${hub.name}/SignalRHubWrapper";`).join("\n")}
`;

  fs.writeFileSync(resolve(OUTPUT_DIR_ROOT, "index.ts"), indexContent);
  console.log("✅ Индексный файл создан (единый источник типов)");
}

// Функция для форматирования сгенерированных файлов с помощью Prettier
async function formatGeneratedFiles() {
  console.log("\n🎨 Форматируем сгенерированные файлы с помощью Prettier...");

  return new Promise(resolve => {
    // Запускаем Prettier для папки api
    const prettierCmd = `yarn prettier --write "src/shared/api/**/*.{ts,tsx}"`;

    exec(prettierCmd, { cwd: process.cwd() }, (error, stdout, stderr) => {
      if (error) {
        console.warn(
          `⚠️ Prettier завершился с предупреждением: ${error.message}`
        );
        // Не прерываем процесс, так как это не критично
        resolve();
        return;
      }

      if (stderr) {
        console.log(`   ${stderr.trim()}`);
      }

      console.log("✅ Prettier форматирование завершено");
      resolve();
    });
  });
}

// Основная функция
async function main() {
  try {
    console.log("🚀 Начинаем генерацию API с разделением типов и клиентов...");

    // Очищаем папку API перед генерацией
    cleanApiDirectory();

    // Читаем swagger файлы для контроллеров и хабов
    const swaggerApiJsonPath = resolve(process.cwd(), "./api/swagger_api.json");
    const swaggerHubsJsonPath = resolve(
      process.cwd(),
      "./api/swagger_hubs.json"
    );

    const swaggerApiSource = JSON.parse(fs.readFileSync(swaggerApiJsonPath));
    const swaggerHubsSource = JSON.parse(fs.readFileSync(swaggerHubsJsonPath));

    // Определяем контроллеры и хабы
    CONTROLLERS = getControllersFromSwagger(swaggerApiSource);
    SIGNALR_HUBS = getSignalRHubsFromSwagger(swaggerHubsSource);

    console.log(`📋 Найдено контроллеров: ${CONTROLLERS.join(", ")}`);
    console.log(
      `📋 Найдено SignalR хабов: ${SIGNALR_HUBS.map(hub => `${hub.name} (${hub.path})`).join(", ")}`
    );

    // Создаем утилиты конфигурации API
    createApiConfig();

    // Генерируем HTTP клиенты (они создают data-contracts.ts в http-clients/)
    await generateHttpClients(prepareSwaggerSpecForGeneration());

    // Копируем data-contracts в папку types после генерации HTTP клиентов
    await generateDataContracts();

    // ВАЖНО: Объединяем SignalR типы в data-contracts ПОСЛЕ его создания
    await mergeSignalRTypesIntoDataContracts();

    await generateSignalRClients();

    // Не создаем файлы обратной совместимости: используем единый источник типов в ./types

    // Создаем индексный файл
    createIndexFile();

    // Форматируем все сгенерированные файлы с помощью Prettier
    await formatGeneratedFiles();

    console.log("\n" + "=".repeat(60));
    console.log("🎉 Генерация API завершена успешно!");
    console.log("=".repeat(60));
    console.log("\n📁 Структура папок:");
    console.log("   ├─ types/");
    console.log("   │  └─ data-contracts.ts (все типы из API и SignalR)");
    console.log("   ├─ http-clients/ (HTTP клиенты)");
    console.log("   ├─ signalr-clients/ (SignalR клиенты)");
    console.log("   └─ index.ts (экспорты)");

    console.log("\n✨ Улучшения:");
    console.log(
      "   ✓ Все типы *OperationResult заменены на OperationResult<T>"
    );
    console.log("   ✓ Единый источник типов в types/data-contracts.ts");
    console.log("   ✓ Улучшена типизация с Generic типами");
    console.log("   ✓ HTTP и SignalR клиенты используют один файл типов");
    console.log("   ✓ Автоматическое объединение типов из API и Hubs");

    console.log("\n📝 Пример использования:");
    console.log(
      "   import { OperationResult, AutoMessageDto } from '@/shared/api';"
    );
    console.log(
      "   const response: OperationResult<AutoMessageDto> = await api.get();"
    );
    console.log("   if (response.success && response.data) { ... }");
    console.log("\n" + "=".repeat(60) + "\n");
  } catch (error) {
    console.error("\n" + "=".repeat(60));
    console.error("❌ Ошибка при генерации API");
    console.error("=".repeat(60));
    console.error(error);
    console.error("=".repeat(60) + "\n");
    process.exit(1);
  }
}

// Запускаем генерацию
main();