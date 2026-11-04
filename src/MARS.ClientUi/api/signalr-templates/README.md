# SignalR Templates

Кастомные шаблоны для генерации SignalR типов и клиентов.

## Файлы шаблонов

### `api.eta`
Основной шаблон для генерации SignalR API типов. Включает:
- Типы для методов хабов
- Типы для ответов
- Интерфейс SignalRHubs
- Опции подключения

### `routetypes.eta`
Шаблон для типов маршрутов SignalR. Включает:
- Типы для каждого маршрута
- Union типы для всех маршрутов
- Map типы для методов и событий
- Конфигурацию хаба

### `models.eta`
Шаблон для моделей данных SignalR. Включает:
- DTO типы
- Request/Response типы
- Интерфейсы для моделей

## Использование

Шаблоны автоматически используются билдером API при генерации SignalR типов.

## Переменные шаблонов

### Доступные переменные:
- `routeTypes` - типы маршрутов
- `modelTypes` - типы моделей
- `responses` - типы ответов
- `config` - конфигурация
- `utils` - утилиты

### Структура routeType:
```typescript
{
  routeName: string;
  routeDescription: string;
  routeParams: Array<{
    name: string;
    type: string;
    description?: string;
  }>;
  responseType: string;
}
```

### Структура modelType:
```typescript
{
  modelName: string;
  modelDescription: string;
  modelParams: Array<{
    name: string;
    type: string;
    required: boolean;
    description?: string;
  }>;
}
```

## Примеры

### Генерация типа метода:
```typescript
export interface GetCurrentState {
  // параметры метода
}

export interface SignalRHubs {
  GetCurrentState(params: GetCurrentState): Promise<void>;
}
```

### Генерация типа маршрута:
```typescript
export interface GetCurrentStateRoute {
  method: 'GetCurrentState';
  params: GetCurrentState;
  response: void;
}
```
