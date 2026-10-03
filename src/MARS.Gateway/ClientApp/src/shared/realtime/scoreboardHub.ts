import { createHubRegistry } from "./hubRegistry";

/**
 * Реестр адаптера хаба табло.
 *
 * Общий механизм — в `hubRegistry.ts`; здесь только сам реестр и его наглядные
 * имена.
 *
 * Реестр отдельный от оверлейного не для красоты: пути у хабов разные
 * (`hubs/scoreboard` и `hubs/overlay`), и один адаптер на оба означал бы, что
 * подписка табло едет в оверлейный сокет и наоборот.
 */
const registry = createHubRegistry();

export function setScoreboardAdapter(
  adapter: Parameters<typeof registry.set>[0]
): void {
  registry.set(adapter);
}
