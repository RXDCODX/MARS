import type { HubAdapter } from "./hubAdapter";
import { getOverlayAdapter, registry } from "./overlayHub";
import type { OverlayEventArgs, OverlayEventName } from "./overlayEvents";

/**
 * Подписка стора на событие оверлейного хаба.
 *
 * Вариант `useOverlayEvent` для React-компонентов: он читает реестр через
 * `useSyncExternalStore` и переподписывается при его смене. Стору, который живёт
 * весь сеанс приложения, хук недоступен, а читать реестр один раз нельзя.
 *
 * Почему одного чтения не хватает: стор импортируется раньше, чем поднимается
 * хаб. Хаб открывает первый компонент, вызвавший `startHub`, а к тому моменту
 * тело `create(...)` стора уже отработало, и в реестре был `null`. Подписка не
 * создавалась, и событие не приходило никогда — при подключённом хабе и
 * «зелёном» стенде.
 *
 * Возвращается отписка. Стор её не зовёт: он живёт дольше соединения, и хаб
 * поднимается один раз. Но тест и размонтирование — зовут, иначе подписка
 * осталась бы в адаптере навсегда.
 */
export function subscribeToOverlayEvent<K extends OverlayEventName>(
  event: K,
  handler: (...args: OverlayEventArgs[K]) => void
): () => void {
  let unsubscribe: (() => void) | null = null;

  const attach = (adapter: HubAdapter): void => {
    unsubscribe = adapter.on(event, handler);
  };

  const current = getOverlayAdapter();

  if (current !== null) {
    attach(current);
  }

  const stopWatching = registry.subscribe(() => {
    // Смена адаптера: старая подписка больше не имеет смысла, а новая нужна
    // сразу. Иначе после переподключения события замолчали бы навсегда.
    unsubscribe?.();
    unsubscribe = null;

    const next = getOverlayAdapter();

    if (next !== null) {
      attach(next);
    }
  });

  return () => {
    stopWatching();
    unsubscribe?.();
    unsubscribe = null;
  };
}

/**
 * Ожидает оверлейный адаптер.
 *
 * Нужна сторам, которые обязаны получить адаптер, а не просто подписаться на его
 * появление: MikuMonday по нему шлёт `MikuMondayTracks` и
 * `DecrementAvailableMikuTrack`.
 *
 * Порядок на стенде такой: React выполняет эффекты детей раньше родительских, и
 * компонент внутри обёртки оверлея смонтировался раньше, чем владелец соединения
 * положил адаптер в реестр. Без ожидания стор читал реестр один раз, получал
 * `null` и поднимал второе соединение к тому же хабу.
 *
 * Ожидание ограничено по времени: если оверлейное соединение так и не поднялось,
 * ждать вечно нельзя — вызывающий должен получить `null` и решить, что делать.
 * Стор при этом остаётся подписанным и заработает, как только адаптер появится.
 */
export function whenOverlayAdapterReady(
  timeoutMs = 10000
): Promise<HubAdapter | null> {
  const current = getOverlayAdapter();

  if (current !== null) {
    return Promise.resolve(current);
  }

  return new Promise<HubAdapter | null>(resolve => {
    let settled = false;

    const finish = (adapter: HubAdapter | null): void => {
      if (settled) {
        return;
      }

      settled = true;
      stopWatching();
      clearTimeout(timer);
      resolve(adapter);
    };

    const stopWatching = registry.subscribe(() => {
      const next = getOverlayAdapter();

      if (next !== null) {
        finish(next);
      }
    });

    // `globalThis.setTimeout`, а не `setTimeout`: тип таймера берётся у функции
    // возврата, иначе `vite/client` подтягивает типы Node и таймер перестаёт быть
    // числом.
    const timer: ReturnType<typeof setTimeout> = globalThis.setTimeout(() => {
      finish(null);
    }, timeoutMs);
  });
}
