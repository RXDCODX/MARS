# MikuMonday Component

Компонент для отображения алертов Miku Monday с управлением через Context API и useReducer.

## Архитектура

### 📁 Структура файлов

```text
MikuMonday/
├── context/
│   ├── MikuMondayContext.tsx   # Context Provider с SignalR логикой
│   └── index.ts                # Экспорты контекста
├── store/
│   ├── mikuMondayStore.ts      # [DEPRECATED] Старый Zustand стор
│   └── index.ts
├── MikuMonday.tsx              # Основной компонент отображения
├── MikuMondayController.tsx    # Контроллер с инициализацией SignalR
├── MikuMonday.module.scss      # Стили
├── MikuMonday.stories.tsx      # Storybook stories
└── index.ts                    # Главный экспорт
```

## Использование

### Вариант 1: С контроллером (рекомендуется)

```tsx
import MikuMondayController from "@/overlays/MikuMonday";

function App() {
  return <MikuMondayController />;
}
```

### Вариант 2: Ручное использование Provider

```tsx
import { MikuMondayProvider, MikuMonday } from "@/overlays/MikuMonday";

function App() {
  return (
    <MikuMondayProvider>
      <MikuMonday />
    </MikuMondayProvider>
  );
}
```

### Вариант 3: Использование контекста в других компонентах

```tsx
import { useMikuMonday } from "@/overlays/MikuMonday";

function CustomComponent() {
  const { state, dispatch, decrementAvailableTrack } = useMikuMonday();
  const { currentAlert, availableTracksCount, isAlertShowing } = state;

  return (
    <div>
      <p>Осталось треков: {availableTracksCount}</p>
      <button onClick={() => dispatch({ type: "CLEAR_QUEUE" })}>
        Очистить очередь
      </button>
    </div>
  );
}
```

## Context API

### State

```typescript
interface MikuMondayState {
  // SignalR подключение
  connection: HubConnection | null;
  isConnected: boolean;

  // Свободные треки
  availableTracks: MikuTrackDto[];
  availableTracksCount: number;

  // Очередь алертов
  alerts: MikuMondayDto[];
  currentAlert: MikuMondayDto | null;
  isAlertShowing: boolean;
}
```

### Actions (useDispatch)

| Action Type             | Payload                         | Описание                               |
| ----------------------- | ------------------------------- | -------------------------------------- |
| `SET_CONNECTION`        | `{ connection: HubConnection }` | Устанавливает SignalR соединение       |
| `SET_CONNECTED`         | `boolean`                       | Устанавливает статус подключения       |
| `SET_AVAILABLE_TRACKS`  | `MikuTrackDto[]`                | Обновляет список свободных треков      |
| `DECREMENT_TRACK_COUNT` | -                               | Уменьшает счетчик треков на 1          |
| `ADD_ALERT`             | `MikuMondayDto`                 | Добавляет новый алерт в очередь        |
| `SHOW_NEXT_ALERT`       | -                               | Показывает следующий алерт из очереди  |
| `CLEAR_CURRENT_ALERT`   | -                               | Очищает текущий алерт                  |
| `CLEAR_QUEUE`           | -                               | Очищает всю очередь                    |
| `RESET`                 | -                               | Сбрасывает state в начальное состояние |

### Методы

#### `fetchAvailableTracks()`

Получает список свободных треков с сервера через SignalR.

```tsx
const { fetchAvailableTracks } = useMikuMonday();
await fetchAvailableTracks();
```

#### `decrementAvailableTrack()`

Уменьшает количество свободных треков и обновляет список если треки закончились.

```tsx
const { decrementAvailableTrack } = useMikuMonday();
await decrementAvailableTrack();
```

## SignalR Integration

### Серверные методы (Hub Methods)

Provider автоматически вызывает следующие методы на сервере:

- `GetAvailableMikuTracks` - получение списка свободных треков
- `DecrementAvailableMikuTrack` - уменьшение счетчика треков

### Серверные события (Hub Events)

Provider подписывается на следующие события:

- `MikuMonday` - получение нового алерта

## Жизненный цикл

1. **Монтирование `MikuMondayProvider`**
   - Создается SignalR подключение
   - Регистрируется обработчик события `MikuMonday`
   - Подключение устанавливается
   - Автоматически загружается список свободных треков

2. **Получение алерта**
   - Событие `MikuMonday` триггерит `ADD_ALERT` action
   - Если нет активного алерта → показывается немедленно
   - Если есть активный алерт → добавляется в очередь

3. **Завершение показа алерта**
   - Вызывается `decrementAvailableTrack()`
   - Dispatch `SHOW_NEXT_ALERT` action
   - Показывается следующий алерт из очереди (если есть)

4. **Размонтирование**
   - SignalR подключение останавливается
   - State сбрасывается в начальное состояние

## Миграция со старого стора

### Было (Zustand store)

```tsx
import { useMikuMondayStore } from "./store";

const { currentAlert, start, dequeueCurrent } = useMikuMondayStore();

useEffect(() => {
  start();
}, [start]);
```

### Стало (Context API)

```tsx
import { useMikuMonday } from "./context/MikuMondayContext";

const { state, dispatch } = useMikuMonday();
const { currentAlert } = state;

// SignalR подключение автоматическое в Provider
dispatch({ type: "SHOW_NEXT_ALERT" });
```

## Преимущества новой архитектуры

- ✅ **Разделение ответственности**: Provider управляет подключением, компонент только отображает
- ✅ **React-way паттерн**: useReducer + Context API
- ✅ **Автоматическое управление подключением**: не нужно вручную вызывать start()
- ✅ **Типобезопасность**: TypeScript типы для всех actions
- ✅ **Гибкость**: можно использовать контекст в любых дочерних компонентах
- ✅ **Предсказуемое состояние**: все изменения через dispatch actions
- ✅ **Легкое тестирование**: можно mock-ить Provider для тестов

## Примеры использования

### Очистка очереди

```tsx
const { dispatch } = useMikuMonday();

<button onClick={() => dispatch({ type: "CLEAR_QUEUE" })}>
  Очистить очередь
</button>;
```

### Отображение количества алертов в очереди

```tsx
const { state } = useMikuMonday();

<div>В очереди: {state.alerts.length} алертов</div>;
```

### Пропуск текущего алерта

```tsx
const { dispatch } = useMikuMonday();

<button onClick={() => dispatch({ type: "SHOW_NEXT_ALERT" })}>
  Следующий алерт
</button>;
```
