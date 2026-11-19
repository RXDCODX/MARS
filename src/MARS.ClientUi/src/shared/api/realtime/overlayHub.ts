import { createHubRegistry } from "./hubRegistry";

/**
 * Реестр адаптера хаба оверлея.
 *
 * Общий механизм — в `hubRegistry.ts`; здесь только сам реестр и его
 * наглядные имена.
 */
/**
 * Реестр оверлейного хаба.
 *
 * Экспортируется и сам реестр, а не только функции доступа: `subscribeToOverlayEvent`
 * должен и подписаться на смену адаптера, и прочитать текущий. Наружу отдаются
 * обёртки, а не сам объект — иначе любой может положить в реестр что угодно.
 */
export const registry = createHubRegistry();

export function setOverlayAdapter(
  adapter: Parameters<typeof registry.set>[0]
): void {
  registry.set(adapter);
}

export function subscribeToOverlayAdapter(
  onStoreChange: () => void
): () => void {
  return registry.subscribe(onStoreChange);
}

/**
 * Адаптер или `null`, если хаб ещё не подключён.
 *
 * `null` означает, что компонент смонтировался раньше соединения. Подписка при
 * этом не теряется: хук читает значение через `useSyncExternalStore` и
 * подпишется, как только стор положит адаптер.
 */
export function getOverlayAdapter(): ReturnType<typeof registry.get> {
  return registry.get();
}
