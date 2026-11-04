# Настройка видео в Storybook

## Проблема

При сборке Storybook и публикации на GitHub Pages видео файлы не отображались из-за неправильной обработки путей к статическим файлам.

## Решение

### 1. Конфигурация Storybook

В `.storybook/main.ts` добавлены настройки:

```typescript
"staticDirs": [
  "../public",
  "../src/components/FumoFriday/FumosVideos"
],
```

### 2. Импорт видео как модули

Создан файл `videoAssets.ts` для импорта видео файлов:

```typescript
import cirnoVideo from "./FumosVideos/cirno.webm";
import reimuVideo from "./FumosVideos/reimu.webm";

export const videoAssets = {
  cirno: cirnoVideo,
  reimu: reimuVideo,
};
```

### 3. Функция с fallback

Функция `getVideoPath()` обеспечивает надежную работу:

```typescript
export const getVideoPath = (videoName: "cirno" | "reimu"): string => {
  if (videoName in videoAssets) {
    return videoAssets[videoName as keyof typeof videoAssets];
  }
  return `./FumosVideos/${videoName}.webm`;
};
```

### 4. Конфигурация Vite

В `vite.config.ts` добавлены настройки для обработки видео:

```typescript
assetsInclude: ['**/*.webm', '**/*.mp4'],
build: {
  rollupOptions: {
    output: {
      assetFileNames: (assetInfo) => {
        const info = assetInfo.name.split('.');
        const ext = info[info.length - 1];
        if (/webm|mp4/.test(ext)) {
          return `assets/videos/[name]-[hash][extname]`;
        }
        return `assets/[name]-[hash][extname]`;
      },
    },
  },
},
```

### 5. TypeScript декларации

В `src/vite-env.d.ts` добавлены типы для видео файлов:

```typescript
declare module "*.webm" {
  const src: string;
  export default src;
}
```

## Использование

В компонентах используйте:

```typescript
import { getVideoPath } from "./videoAssets";

<video src={getVideoPath('cirno')} autoPlay controls={false} />
```

## Результат

- ✅ Видео работает в локальной разработке
- ✅ Видео работает в собранном Storybook
- ✅ Видео работает на GitHub Pages
- ✅ Правильная обработка хешированных имен файлов
- ✅ Fallback на статические пути при необходимости
