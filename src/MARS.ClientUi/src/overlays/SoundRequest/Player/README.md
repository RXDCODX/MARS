# SoundRequest Player

Адаптивный плеер для управления системой звуковых запросов (SoundRequest).

## Описание

Плеер автоматически определяет разрешение экрана и рендерит соответствующий интерфейс:

- **Десктопная версия** (≥1024px) - полный интерфейс с расширенными возможностями
- **Мобильная версия** (≤768px) - компактный интерфейс, оптимизированный для сенсорного управления

## Структура компонентов

```
Player/
├── SoundRequestPlayer.tsx              # Адаптивный компонент-обертка
├── Background/
│   ├── LiquidChrome.tsx               # WebGL анимированный фон
│   ├── LiquidChrome.css
│   └── index.ts
├── Desktop/
│   ├── SoundRequestPlayerDesktop.tsx  # Десктопная версия (только UI)
│   ├── SoundRequestPlayerDesktop.module.scss
│   └── SoundRequestPlayerDesktop.stories.tsx
├── Mobile/
│   ├── SoundRequestPlayerMobile.tsx   # Мобильная версия (только UI)
│   ├── SoundRequestPlayerMobile.module.scss
│   └── SoundRequestPlayerMobile.stories.tsx
├── hooks/
│   ├── useSoundRequestPlayer.ts       # Общая логика управления плеером
│   └── index.ts
├── utils/
│   ├── playerUtils.ts                 # Утилитарные функции
│   └── index.ts
└── index.ts                            # Экспорты
```

## Использование

### Базовое использование

```tsx
import { SoundRequestPlayer } from "@/overlays/SoundRequest/Player";

function MyComponent() {
  return <SoundRequestPlayer />;
}
```

### Использование конкретной версии

```tsx
import { SoundRequestPlayerDesktop } from "@/overlays/SoundRequest/Player";
// или
import { SoundRequestPlayerMobile } from "@/overlays/SoundRequest/Player";
```

## Функциональность

### Управление воспроизведением

- ▶️ **Play** - начать воспроизведение
- ⏸️ **Pause** - приостановить воспроизведение
- ⏹️ **Stop** - остановить воспроизведение
- ⏭️ **Skip** - пропустить текущий трек

### Управление громкостью

- 🔊 **Volume** - регулировка громкости (0-100%)
- 🔇 **Mute** - включить/выключить звук

### Управление очередью

- Просмотр списка треков в очереди
- Удаление треков из очереди
- Отображение информации о треках (название, исполнитель, запросивший пользователь)

## API

Плеер использует следующие API методы из `SoundRequest.ts`:

### Получение данных

- `soundRequestStateList()` - получение состояния плеера
- `soundRequestQueueList()` - получение списка треков в очереди

### Управление воспроизведением

- `soundRequestPlayCreate()` - воспроизведение
- `soundRequestPauseCreate()` - пауза
- `soundRequestStopCreate()` - остановка
- `soundRequestSkipCreate()` - пропуск трека

### Управление громкостью

- `soundRequestVolumeCreate(volume)` - установка громкости
- `soundRequestMuteCreate(muted)` - включение/выключение звука

### Управление очередью

- `soundRequestQueueDelete(trackId)` - удаление трека из очереди

## Уведомления

Все операции используют систему уведомлений `ToastModal`:

- Успешные операции отображаются с зеленым тостом
- Ошибки отображаются с красным тостом
- При клике на тост открывается модальное окно с детальной информацией

## Автообновление

Плеер автоматически обновляет данные каждые 2 секунды:

- Состояние плеера (текущий трек, статус воспроизведения)
- Список треков в очереди

## Адаптивность

### Десктопная версия

- Широкий интерфейс с горизонтальной компоновкой
- Все элементы управления видны одновременно
- Очередь треков отображается в отдельной карточке

### Мобильная версия

- Компактный вертикальный интерфейс
- Кнопки управления расположены в сетке 4x1
- Очередь треков скрывается/показывается по клику
- Оптимизация для сенсорного управления

## Стилизация

### Анимированный фон

Плеер использует компонент `LiquidChrome` для создания динамического WebGL-фона:

- Красивые волны и переливы с эффектом жидкого хрома
- Заполняет весь экран (position: fixed)
- Настраиваемые параметры (цвет, скорость, амплитуда)

### CSS переменные

Плеер использует CSS переменные из `global.scss`:

- `--bs-primary` - основной цвет
- `--bs-secondary` - вторичный цвет
- `--bs-card-bg` - фон карточек
- `--bs-border-color` - цвет границ
- `--bs-box-shadow` - тени

## Архитектура

Плеер построен по принципу разделения логики и представления:

### Хук `useSoundRequestPlayer`

Содержит всю бизнес-логику:

- Управление состоянием плеера
- Загрузка данных с сервера
- Обработчики всех действий (play, pause, skip, volume и т.д.)
- Автообновление данных

### Утилиты `playerUtils`

Вспомогательные функции:

- `formatDuration()` - форматирование времени
- `getAuthorsString()` - форматирование списка авторов
- `getRequestedByString()` - форматирование имени пользователя

### Компоненты Desktop/Mobile

Содержат только UI-логику:

- Верстка интерфейса
- Обработка UI-событий (клики, изменения input)
- Отображение данных из хука

## Зависимости

- `react-bootstrap` - компоненты UI
- `lucide-react` - иконки
- `@/shared/api` - API клиенты
- `@/shared/utils/ToastModal` - система уведомлений
- `@/shared/hooks/useMediaQuery` - определение разрешения экрана

## Примеры использования

### В OBS компоненте

```tsx
import { SoundRequestPlayer } from "@/overlays/SoundRequest/Player";

export function MyOBSComponent() {
  return (
    <div>
      <SoundRequestPlayer />
    </div>
  );
}
```

### В странице сайта

```tsx
import { SoundRequestPlayer } from "@/overlays/SoundRequest/Player";

function SoundRequestPage() {
  return (
    <div className="container">
      <h1>Управление звуковыми запросами</h1>
      <SoundRequestPlayer />
    </div>
  );
}
```

## Разработка

### Добавление новых функций

1. Добавьте новый метод в API (`SoundRequest.ts`)
2. Создайте обработчик в обоих компонентах (Desktop/Mobile)
3. Добавьте UI элемент в соответствующий компонент
4. Обновите документацию

### Тестирование

Используйте Storybook для тестирования компонентов:

```bash
yarn storybook
```

Доступные истории:

- `SoundRequestPlayer` - адаптивный плеер
- `SoundRequestPlayerDesktop` - десктопная версия
- `SoundRequestPlayerMobile` - мобильная версия

## Известные ограничения

- Автообновление происходит каждые 2 секунды (можно настроить)
- Не поддерживается drag-and-drop для изменения порядка треков
- Не отображается прогресс воспроизведения текущего трека

## Лицензия

Часть проекта MARS.
