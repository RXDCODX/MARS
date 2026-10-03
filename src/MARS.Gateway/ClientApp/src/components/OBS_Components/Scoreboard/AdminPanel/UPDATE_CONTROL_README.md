# Исправление бесконечных обновлений в админ панели скорборда

## Проблема

В админ панели скорборда происходили бесконечные обновления из-за:

1. Недостаточного контроля над SignalR событиями
2. Отсутствия четкого разделения между локальными изменениями и серверными обновлениями
3. Множественных подписок на одни и те же события
4. Недостаточной защиты от рекурсии
5. Использования типа `any` в дебаунсинг функциях

## Решение

### 1. Создан хук `useUpdateControl`

Новый хук `useUpdateControl.ts` предоставляет централизованный контроль над обновлениями:

- Отслеживание состояния обновлений через `useRef`
- Генерация уникальных ID для каждого обновления
- Проверка необходимости игнорирования серверных обновлений
- Защита от одновременных обновлений

### 2. Улучшен `useAdminState`

- Интегрирован с `useUpdateControl`
- Добавлены уникальные ID для каждого обновления
- Улучшена логика проверки timestamp'ов
- Увеличено время защиты от рекурсии до 1 секунды

### 3. Оптимизирован `AdminPanel`

- Убраны дублирующие подписки на SignalR события
- Добавлен дебаунсинг для обработчиков событий с правильной типизацией
- Добавлена дополнительная защита от частых обновлений
- Упрощена логика обработки событий
- **Исправлена типизация**: заменен `any` на `Partial<Player>` и `Partial<MetaInfo>`

### 4. Исправлена типизация в `MetaPanel`

- Изменен тип `setMeta` с `MetaInfoWithTimestamp` на `Partial<MetaInfo>`
- Упрощены вызовы функций обновления

## Ключевые изменения

### Контроль обновлений

```typescript
// Новый хук для контроля обновлений
const updateControl = useUpdateControl();

// Проверка необходимости игнорирования обновления
if (updateControl.shouldIgnoreUpdate(now)) {
  return;
}
```

### Уникальные ID обновлений

```typescript
// Генерация уникального ID для каждого обновления
const updateId = updateControl.generateUpdateId();
await sendToServer("UpdateState", serverState, updateId);
```

### Дебаунсинг обработчиков с правильной типизацией

```typescript
// Дебаунсинг для предотвращения частых обновлений
const debouncedSetPlayer1 = useCallback(
  (update: Partial<Player>) => {
    const now = Date.now();
    if (now - lastUpdateRef.current < 50) {
      console.log("Debouncing player1 update");
      return;
    }
    lastUpdateRef.current = now;
    setPlayer1(update);
  },
  [setPlayer1]
);

const debouncedSetMeta = useCallback(
  (update: Partial<MetaInfo>) => {
    const now = Date.now();
    if (now - lastUpdateRef.current < 50) {
      console.log("Debouncing meta update");
      return;
    }
    lastUpdateRef.current = now;
    setMeta(update);
  },
  [setMeta]
);
```

### Улучшенная защита от рекурсии

```typescript
// Проверка времени последнего обновления
const shouldUpdate = !prev._lastEdit || prev._lastEdit < now - 1000;
```

## Результат

- Устранены бесконечные обновления
- Четкое разделение между локальными изменениями и серверными обновлениями
- Улучшена производительность
- Добавлена надежная защита от рекурсии
- Упрощена отладка через подробное логирование
- **Полностью типизированный код без использования `any`**

## Мониторинг

Все обновления логируются в консоль для отладки:

- `Starting update: update_1234567890_abc123`
- `Finished update: update_1234567890_abc123`
- `Ignoring server update while local update is in progress`
- `Debouncing player1 update`
