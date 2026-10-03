import { createHubRegistry } from "./hubRegistry";

/**
 * Реестр адаптера хаба тунца.
 *
 * Общий механизм — в `hubRegistry.ts`; здесь только сам реестр и его наглядные
 * имена.
 *
 * Реестр отдельный от оверлейного не для красоты: путь у хаба другой
 * (`hubs/tuna`), и один адаптер на оба означал бы, что события тунца едут в
 * оверлейный сокет, где их никто не слушает.
 */
const registry = createHubRegistry();

export function setTunaAdapter(adapter: Parameters<typeof registry.set>[0]): void {
  registry.set(adapter);
}

export function getTunaAdapter(): ReturnType<typeof registry.get> {
  return registry.get();
}
