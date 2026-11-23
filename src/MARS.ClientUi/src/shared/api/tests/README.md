# Tests (`src/shared/api/tests`)

Тесты транспортного слоя API (Vitest).

## Файлы

- `operationResult.test.ts` — разбор конверта ответа сервиса (`unwrapOperationResult`): обе формы конверта, ошибки, прохождение не-конвертов.
- `spaFallback.test.ts` — определение HTML-заглушки SPA (`isSpaFallback`).

## Запуск

```bash
cd src/MARS.ClientUi
yarn test
```

Тесты проверяют чистые функции, поэтому не поднимают axios и не мокают сеть. При изменении `http-client.ts` добавляйте/обновляйте тесты здесь.