# SoundRequestPlayerMobile

Мобильная версия плеера для Sound Request системы. Переделана с использованием компонентной архитектуры по аналогии с десктопной версией.

## Структура

```
Mobile/
├── CurrentTrack/          # Отображение текущего трека
│   ├── CurrentTrack.tsx
│   ├── CurrentTrack.module.scss
│   └── index.ts
├── PlayerControls/        # Кнопки управления плеером
│   ├── PlayerControls.tsx
│   ├── PlayerControls.module.scss
│   └── index.ts
├── VolumeControls/        # Управление громкостью и видео режимом
│   ├── VolumeControls.tsx
│   ├── VolumeControls.module.scss
│   └── index.ts
├── QueueItem/            # Отдельный элемент очереди
│   ├── QueueItem.tsx
│   ├── QueueItem.module.scss
│   └── index.ts
├── QueueList/            # Список очереди с возможностью сворачивания
│   ├── QueueList.tsx
│   ├── QueueList.module.scss
│   └── index.ts
├── SoundRequestPlayerMobile.tsx           # Главный компонент
├── SoundRequestPlayerMobile.module.scss   # Основные стили
└── SoundRequestPlayerMobile.stories.tsx   # Storybook истории
```

## Компоненты

### SoundRequestPlayerMobile (главный компонент)

Контейнер для всех подкомпонентов мобильного плеера. Чистый UI компонент - не содержит логики управления.

**Особенности:**

- Мемоизирован для оптимизации производительности
- Использует композицию подкомпонентов
- Следует принципу единой ответственности

### CurrentTrack

Отображает информацию о текущем треке:

- Обложка трека с анимацией воспроизведения
- Название и исполнители
- Пользователь, запросивший трек
- Длительность

**Стор подписки:** `playerState`, `currentQueueItem`

### PlayerControls

Кнопки управления воспроизведением:

- Play/Pause (переключатель)
- Stop
- Skip (пропустить)
- Далее (следующий из очереди)

**Стор подписки:** `isPlaying`, `isStopped`, `loading`, `actions`

### VolumeControls

Управление громкостью и режимом отображения видео:

- Кнопка Mute/Unmute
- Кнопка переключения режима видео (Video/NoVideo/AudioOnly)
- Слайдер громкости

**Стор подписки:** `volume`, `isMuted`, `videoState`, `loading`, `actions`

### QueueItem

Отдельный элемент очереди треков:

- Номер в очереди
- Информация о треке (название, исполнители, заказчик)
- Длительность
- Кнопки действий (Играть/Удалить) - опционально

**Props:**

- `item` - данные трека из очереди
- `index` - позиция в очереди
- `showPlayButton` - показать кнопку "Играть"
- `showDeleteButton` - показать кнопку "Удалить"
- `onPlay` - обработчик воспроизведения
- `onDelete` - обработчик удаления
- `isDeleting` - флаг процесса удаления
- `isLoading` - флаг загрузки

### QueueList

Список очереди с функционалом:

- Сворачивание/разворачивание (опционально)
- Ограничение количества элементов
- Отображение кнопок действий
- Пустое состояние

**Props:**

- `title` - заголовок списка
- `limit` - максимальное количество треков для отображения
- `showPlayButton` - показать кнопку "Играть" у элементов
- `showDeleteButton` - показать кнопку "Удалить" у элементов
- `collapsible` - возможность сворачивания
- `defaultExpanded` - начальное состояние (свёрнут/развёрнут)

**Стор подписки:** `queue`, `loading`

## Архитектурные принципы

1. **Разделение ответственности** - каждый компонент отвечает за одну функцию
2. **Переиспользование** - компоненты можно использовать независимо
3. **Оптимизация** - все компоненты мемоизированы
4. **Селективная подписка** - используется `useShallow` для предотвращения лишних ререндеров
5. **Чистота** - UI компоненты не содержат бизнес-логики, только отображение и вызов actions

## Использование стора

Компоненты используют `usePlayerStore` с селективной подпиской через `useShallow`:

```tsx
const { volume, actions } = usePlayerStore(
  useShallow(state => ({
    volume: state.volume,
    actions: state.actions,
  }))
);
```

Это предотвращает ререндер компонента при изменении других полей стора.

## Actions

Все действия выполняются через объект `actions` из стора:

- `handleTogglePlayPause()` - переключение Play/Pause
- `handleStop()` - остановка воспроизведения
- `handleSkip()` - пропустить трек
- `handlePlayNext()` - играть следующий из очереди
- `handleMute()` - включить/выключить звук
- `handleToggleVideoState()` - переключение режима видео
- `handleVolumeChange(value)` - изменение громкости

## Хуки

### useQueueActions

Хук для работы с очередью, предоставляет:

- `handleItemHover` - синхронизация hover эффектов
- `handleDeleteFromQueue` - удаление трека с оптимистичным обновлением
- `deletingId` - ID удаляемого трека

## Стилизация

Все стили используют CSS модули и переменные Bootstrap:

- `var(--bs-primary)` - основной цвет
- `var(--bs-secondary)` - вторичный цвет
- `var(--bs-light)` - светлый фон
- `var(--bs-card-bg)` - фон карточки
- `var(--bs-border-color)` - цвет границ
- `var(--bs-text-muted)` - приглушённый текст

## Отличия от Desktop версии

1. **Упрощённая структура** - нет разделения на колонки треков и пользователей
2. **Карточная вёрстка** - использование Bootstrap Card для секций
3. **Вертикальный лейаут** - все элементы расположены вертикально
4. **Сворачиваемые секции** - очередь может сворачиваться для экономии места
5. **Мобильная оптимизация** - большие кнопки, адаптивная типографика
6. **Отсутствие истории** - не реализован режим с историей треков

## Примеры использования

### Базовое использование

```tsx
import { SoundRequestPlayerMobile } from "./Mobile";

function App() {
  return <SoundRequestPlayerMobile />;
}
```

### С дополнительной обёрткой

```tsx
import { SoundRequestPlayerMobile } from "./Mobile";

function MobilePage() {
  return (
    <div className="mobile-page">
      <SoundRequestPlayerMobile />
    </div>
  );
}
```
