import { createHubRegistry } from "./hubRegistry";

/**
 * Реестр адаптера хаба очереди звуковых запросов.
 *
 * Общий механизм — в `hubRegistry.ts`; здесь только сам реестр и его наглядные
 * имена.
 */
const registry = createHubRegistry();

export function setSoundRequestAdapter(
  adapter: Parameters<typeof registry.set>[0]
): void {
  registry.set(adapter);
}
