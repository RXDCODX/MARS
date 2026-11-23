# FumoFriday Component

Компонент для отображения алертов FumoFriday с анимированными видео.

## Особенности

- Использует локальные видео файлы из папки `FumosVideos`
- Поддерживает два типа алертов: Cirno и Reimu
- Автоматическое переключение между типами алертов
- Интегрированная кнопка для тестирования
- Глобальная функция для программного тестирования

## Использование

### Базовое использование

```tsx
import { FumoFriday } from "./components/FumoFriday";

function App() {
  return <FumoFriday />;
}
```

### Тестирование

#### Способ 1: Кнопка в интерфейсе

На странице FumoFriday есть кнопка "Test FumoFriday Alert" в правом верхнем углу.

#### Способ 2: Программное тестирование

```javascript
// В консоли браузера
window.testFumoFriday();

// Или программно
import { testFumoFridayAlert } from "./components/FumoFriday/FumoFriday.test";
testFumoFridayAlert();
```

#### Способ 3: Кастомное сообщение

```javascript
import { testFumoFridayWithCustomMessage } from "./components/FumoFriday/FumoFriday.test";
testFumoFridayWithCustomMessage("Custom User", "#4ecdc4");
```

## Структура файлов

```text
FumoFriday/
├── FumoFriday.tsx              # Основной компонент
├── FumoFridayController.tsx    # Контроллер логики
├── Cirno.tsx                   # Компонент алерта Cirno
├── Reimu.tsx                   # Компонент алерта Reimu
├── FumosVideos/                # Папка с видео файлами
│   ├── cirno.webm
│   └── reimu.webm
├── Styles.module.scss          # Стили компонента
├── FumoFriday.test.ts          # Тестовые функции
└── index.ts                    # Экспорты
```

## API

### Message Interface

```typescript
interface Message {
  id: string;
  message: string;
  color?: string;
}
```

### Глобальные функции

- `window.testFumoFriday()` - Запускает тестовый алерт

### Экспортируемые функции

- `testFumoFridayAlert()` - Программное тестирование
- `testFumoFridayWithCustomMessage(message, color)` - Тестирование с кастомными параметрами

## Стили

Компонент использует CSS Modules с файлом `Styles.module.scss`. Основные классы:

- `.testControls` - Контейнер для кнопки тестирования
- `.testButton` - Стили кнопки тестирования
- `.box-container` - Основной контейнер алерта
- `.container` - Внутренний контейнер
- `.text` - Стили для текста

## Зависимости

- React
- uuid (для генерации уникальных ID)
- react-textfit (для адаптивного текста)
- SignalR (для получения сообщений с сервера)
