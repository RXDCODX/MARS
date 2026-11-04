import { useCallback } from "react";

import type { HubAdapter, HubInvocationMap } from "./hubAdapter";
import { useOverlayAdapter } from "./useOverlayAdapter";

/**
 * Методы, о неудаче которых уже сказано.
 *
 * Оверлей зовёт `MuteAll` при каждом алерте, а метода у хаба нет: серверная
 * часть не реализована вовсе, звать gRPC-сервис неоткуда. Без этого набора
 * каждое срабатывание алерта писало бы в консоль одно и то же, а оверлей —
 * это фон поверх видео, и он не должен ни падать, ни засорять вывод.
 */
const reportedFailures = new Set<string>();

/**
 * Вызов клиентского метода хаба из компонента.
 *
 * Раньше это был `SignalRContext.invoke("MuteAll", [])` — строка без проверки,
 * и опечатка проходила компиляцию. Здесь имя берётся из карты
 * `HubInvocationMap`, поэтому `invoke("MuteAl")` не собирается.
 *
 * Адаптер берётся из того же реестра, что и у `useOverlayEvent`: оба хука
 * смотрят на один источник, иначе подписка и вызов разошлись бы по разным
 * соединениям.
 *
 * Пока хаб не подключён, вызов не делается и ошибки не бросается. Раньше вызов
 * без соединения падал, и компонент оверлея — а это фон, чистый фон поверх
 * видео — падал вместе с ним. Отсутствие канала на экране безопасно: событие
 * отправки всё равно никому не дошло бы.
 *
 * Ошибка самого вызова не пробрасывается: сигнатура обещает `Promise<void>`,
 * а вызывающий почти нигде её не ждёт, и отказ превращался в «Unhandled promise
 * rejection» в консоли браузера. Сообщение печатается один раз на метод и
 * уровнем ниже `error` — E2E-проверка маршрутов считает ошибкой именно `error`,
 * а срывать её из-за нереализованной кнопки было бы ложью: нереализовано
 * ровно то, что перечислено в `hubAdapter.ts`.
 */
export function useHubInvoke(): <K extends keyof HubInvocationMap>(
  method: K,
  ...args: HubInvocationMap[K]
) => Promise<void> {
  const adapter = useOverlayAdapter();

  return useCallback(
    async (method, ...args) => {
      if (adapter === null) {
        return;
      }

      try {
        await (adapter as HubAdapter).invoke(method, ...args);
      } catch (error) {
        if (!reportedFailures.has(method)) {
          reportedFailures.add(method);

          console.warn(
            `[hub] Метод ${method} не обслуживается хабом, вызов пропущен.`,
            error
          );
        }
      }
    },
    [adapter]
  );
}

/**
 * Сбрасывает память о неудачных вызовах. Нужен тестам.
 *
 * Без этого тест, проверивший отказ одного метода, влиял бы на все следующие:
 * второй тест того же метода не увидел бы сообщения и решил бы, что его нет.
 */
export function resetReportedHubFailures(): void {
  reportedFailures.clear();
}
