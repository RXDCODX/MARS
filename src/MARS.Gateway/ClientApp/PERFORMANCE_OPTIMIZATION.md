# 🚀 Рекомендации по оптимизации производительности

## 📊 Текущие проблемы

- **228 запросов** при загрузке страницы
- **20MB transferred / 21.2MB resources**
- **27 секунд** время загрузки (Finish: 27.65s)
- Каждый `.module.scss` файл загружается отдельно
- Множество SVG файлов загружается отдельно
- Дублирующиеся запросы к `index.ts`

## ✅ Приоритетные оптимизации

### 1. **Lazy Loading для всех страниц** (КРИТИЧНО!)

**Проблема:** Многие страницы загружаются сразу при старте приложения.

**Решение:**

```typescript
// mainSiteRoutes.tsx - сделать все страницы lazy
const WelcomePage = lazy(() => import("@/Site/Pages/WelcomePage"));
const AboutPage = lazy(() => import("@/Site/Pages/AboutPage"));
const DocsPage = lazy(() => import("@/Site/Pages/DocsPage"));
const ContactsPage = lazy(() => import("@/Site/Pages/ContactsPage"));
const RoutesPage = lazy(() => import("@/Site/Pages/RoutesPage/RoutesPage"));

// В роутах обернуть в Suspense
<Suspense fallback={<PageLoader />}>
  <WelcomePage />
</Suspense>
```

**Ожидаемый эффект:** Снижение начальной загрузки на 40-60%

### 2. **Оптимизация SVG файлов**

**Проблема:** 255 SVG файлов загружаются отдельно.

**Решения:**

#### Вариант A: SVG Sprite

```typescript
// Создать один SVG sprite файл со всеми иконками
import './icons-sprite.svg';

// Использовать:
<svg>
  <use href="#icon-name" />
</svg>
```

#### Вариант B: Inline SVG для маленьких иконок

```typescript
// Вместо импорта файла
const Icon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24">
    <path d="..." />
  </svg>
);
```

#### Вариант C: Использовать icon font (bootstrap-icons уже установлен)

```typescript
// Вместо SVG использовать:
<i className="bi bi-play-fill" />
```

**Ожидаемый эффект:** Снижение количества запросов на 200+

### 3. **Объединение CSS модулей**

**Проблема:** Каждый компонент загружает свой `.module.scss` отдельно.

**Решения:**

#### Вариант A: Глобальные стили для общих компонентов

```scss
// Вместо множества модулей создать shared.module.scss
@import "@/app/global.scss";

.shared-button {
  // общие стили
}

.shared-card {
  // общие стили
}
```

#### Вариант B: CSS-in-JS для маленьких компонентов

```typescript
// Для компонентов с минимальными стилями
const styles = {
  container: {
    display: 'flex',
    gap: '1rem'
  }
};
```

**Ожидаемый эффект:** Снижение количества запросов на 30-50%

### 4. **Оптимизация Vite конфигурации**

Добавить в `vite.config.ts`:

```typescript
export default defineConfig({
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          // Добавить больше vendor chunks
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'ui-vendor': ['react-bootstrap', 'bootstrap'],
          'icons-vendor': ['lucide-react', 'bootstrap-icons'],
          'signalr-vendor': ['@microsoft/signalr', 'react-signalr'],
          'charts-vendor': ['recharts'],
          'animations-vendor': ['framer-motion', 'react-simple-animate'],
          'twitch-vendor': ['@twurple/api', '@twurple/auth', '@twurple/chat'],
        },
      },
    },
    // Включить minification
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true, // Удалить console.log в production
      },
    },
    // Включить source maps только для development
    sourcemap: process.env.NODE_ENV === 'development',
  },
  // Включить compression
  plugins: [
    react(),
    // Добавить vite-plugin-compression
    viteCompression({
      algorithm: 'gzip',
      ext: '.gz',
    }),
  ],
});
```

### 5. **Code Splitting для тяжелых компонентов**

```typescript
// Для компонентов с большими зависимостями
const HeavyComponent = lazy(() => 
  import('./HeavyComponent').then(module => ({
    default: module.HeavyComponent
  }))
);
```

### 6. **Оптимизация изображений**

**Проблема:** Неоптимизированные изображения (321 .jpg файл).

**Решения:**

- Конвертировать в WebP формат (меньше размер на 25-35%)
- Использовать responsive images
- Lazy loading для изображений вне viewport

```typescript
<img 
  src="image.webp" 
  loading="lazy"
  decoding="async"
  alt="..."
/>
```

### 7. **Удаление неиспользуемых зависимостей**

Проверить и удалить:

- Неиспользуемые библиотеки
- Дублирующиеся зависимости
- Dev зависимости в production bundle

```bash
# Проверить размер bundle
yarn build --analyze

# Найти неиспользуемые зависимости
npx depcheck
```

### 8. **Оптимизация SignalR соединений**

**Проблема:** Множество WebSocket соединений открывается сразу.

**Решение:**

```typescript
// Открывать соединения только при необходимости
const useSignalR = (hubName: string) => {
  const [connection, setConnection] = useState(null);
  
  useEffect(() => {
    // Создавать соединение только когда компонент видим
    if (isVisible) {
      const conn = new HubConnectionBuilder()
        .withUrl(`/hubs/${hubName}`)
        .build();
      setConnection(conn);
      
      return () => conn.stop();
    }
  }, [isVisible]);
};
```

### 9. **Preloading критических ресурсов**

```typescript
// В index.html добавить preload для критических ресурсов
<link rel="preload" href="/fonts/main.woff2" as="font" type="font/woff2" crossorigin />
<link rel="preload" href="/assets/critical.css" as="style" />
```

### 10. **Service Worker для кеширования**

```typescript
// vite.config.ts
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
      },
    }),
  ],
});
```

## 📈 Ожидаемые результаты

После внедрения всех оптимизаций:

| Метрика | До | После | Улучшение |
|---------|-----|-------|-----------|
| Запросы | 228 | ~50-70 | **-70%** |
| Размер | 20MB | ~5-8MB | **-60%** |
| Время загрузки | 27s | ~3-5s | **-80%** |
| First Contentful Paint | ? | <1.5s | - |
| Time to Interactive | ? | <3s | - |

## 🎯 План внедрения (по приоритету)

### Фаза 1 (Критично - 1-2 дня)

1. ✅ Lazy loading для всех страниц
2. ✅ SVG optimization (sprite или icon font)
3. ✅ Объединение CSS модулей

### Фаза 2 (Важно - 3-5 дней)

4. ✅ Оптимизация Vite конфигурации
5. ✅ Code splitting для тяжелых компонентов
6. ✅ Оптимизация изображений

### Фаза 3 (Желательно - 1 неделя)

7. ✅ Service Worker
8. ✅ Preloading критических ресурсов
9. ✅ Оптимизация SignalR соединений

## 🔍 Мониторинг

После внедрения проверять:

- Chrome DevTools > Network
- Chrome DevTools > Lighthouse
- WebPageTest.org
- Bundle Analyzer

## 📚 Полезные ресурсы

- [Vite Performance Optimization](https://vitejs.dev/guide/performance.html)
- [Web.dev Performance](https://web.dev/performance/)
- [React Performance Optimization](https://react.dev/learn/render-and-commit)
- [Bundle Analyzer](https://www.npmjs.com/package/rollup-plugin-visualizer)
