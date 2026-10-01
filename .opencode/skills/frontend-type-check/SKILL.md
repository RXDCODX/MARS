---
name: frontend-type-check
description: Type-check and build the MARS MediaStorage UI (React + Vite) with npm. Use when the user asks to check types, build the frontend, or verify the storage UI compiles.
---

# Frontend Type Check & Build

Единственный фронтенд репозитория —
`src/MARS.MediaStorage/ClientApp` (React 19 + Vite 7 + TypeScript 5.7). Никакого
`mars.client` из монолита здесь нет.

**Пакетный менеджер — `npm`**, не `yarn`: в `ClientApp` лежит `package-lock.json`,
и `yarn` сломает воспроизводимость сборки образа.

## Workflow

### 1. Зависимости

```bash
cd src/MARS.MediaStorage/ClientApp
npm ci
```

`npm ci` — всегда, а не `npm install`: в Docker-образе собирается lock-файл,
и расхождение с ним роняет сборку.

### 2. Проверка типов

```bash
npm run typecheck
```

Это `tsc -b --noEmit` по проектам `tsconfig.app.json` и `tsconfig.node.json` —
самый дешёвый способ узнать, что сломалось.

### 3. Сборка

```bash
npm run build
```

`tsc -b && vite build`. Результат идёт в `../ui-dist/` (gitignored), а не в
`wwwroot`: `wwwroot` — git-версионируемый том хранилища, и сборка UI внутри него
породила бы коммит с минифицированными файлами, а сами файлы попали бы в таблицу
записей как «медиа».

### 4. Dev-сервер

```bash
npm run dev
```

Порт `5173`, проксирует `/api` на `http://localhost:9155` (Gateway). Прокси
настроен в `vite.config.ts`; при смене порта Gateway править и его.

## Что проверять руками

`npm run typecheck` не ловит смысловых ошибок, типичных для этого UI:

- `src/api.ts` — единственное место, где ходится в API. Он сам разбирает конверт
  `OperationResult` и **бросает исключение** при `Success = false`. В компонентах
  `fetch` не вызывать, и не разбирать конверт второй раз.
- Состояние — `useState`/`useReducer`. Стор-библиотеки нет: `useStore.getState()`,
  `useShallow` и `ToastModal` из монолита здесь не существуют.
- `useMemo` на filter/sort по массивам, `useCallback` для функций, уходящих в
  зависимости хуков. Виртуализация списка ручная, на `ROW_HEIGHT`/`OVERSCAN`.
- Импорты React именованные: `import { useState } from 'react'`.
- Цвета и стили — из `src/styles.css`, литералы в разметке не хардкодить.
- Состояния loading / error / empty обязательны; см. skill `build-for-good-ux`.

## Линтера нет

ESLint, stylelint и vitest в `ClientApp` не подключены — в `package.json` только
`dev`, `build`, `preview`, `typecheck`. Не ищи `npm run lint` или `npm test`: их
нет, и «исправление» их отсутствия не входит в задачу.

## Notes

- `base: '/storage-ui/'` в `vite.config.ts` — путь отдачи. Он связан с
  `RequestPath` в `Program.cs` хранилища и с правилом `media-storage-ui` в YARP;
  менять в одном месте нельзя.
- Сборка UI выполняется **внутри образа** (stage `node:24-alpine`). Локальный
  `npm run build` нужен для проверки, но в git результат не попадает.