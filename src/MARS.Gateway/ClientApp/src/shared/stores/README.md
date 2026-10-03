# Stores

Директория для хранения глобальных store (состояний) приложения, использующих Zustand.

## WaifuPrizesStore

Store для управления призами рулетки вайфу.

### Использование

```typescript
import { useWaifuPrizesStore } from "@/shared/stores/waifuPrizesStore";

function Component() {
  const { prizes, addPrizes, shuffle, clear } = useWaifuPrizesStore();

  // Использование методов store
  addPrizes([...]); // Добавить призы
  shuffle();        // Перемешать призы
  clear();          // Очистить все призы
}
```

### API

- `prizes: PrizeType[]` - массив призов
- `addPrizes(prizes: PrizeType[])` - добавляет новые призы, исключая дубликаты
- `shuffle()` - перемешивает призы в случайном порядке
- `clear()` - очищает все призы
- `preloadImages(prizes: PrizeType[])` - предзагружает изображения призов

### Особенности

- Автоматически исключает дубликаты при добавлении призов (по полю `id`)
- Предзагружает изображения для оптимизации производительности
- Интегрируется с Redux DevTools для отладки
- Все изменения логируются в консоль
