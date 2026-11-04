// Утилита для предзагрузки компонентов в фоне

import type {
  LazyComponentLoader,
  PrefetchResult,
} from "../types/prefetch.types";

// Компоненты, которые нужно загрузить сразу (критичные)
export const criticalComponents: LazyComponentLoader[] = [];

// Компоненты для фоновой загрузки (некритичные)
export const prefetchComponents: LazyComponentLoader[] = [];

/**
 * Предзагрузка компонентов в фоне
 * Использует requestIdleCallback для загрузки без блокировки UI
 */
export const prefetchNonCriticalComponents = (): Promise<PrefetchResult> => {
  const result: PrefetchResult = {
    loaded: 0,
    total: prefetchComponents.length,
    errors: [],
  };

  if (prefetchComponents.length === 0) {
    return Promise.resolve(result);
  }

  return new Promise<PrefetchResult>(resolve => {
    let index = 0;

    const loadNext = () => {
      if (index >= prefetchComponents.length) {
        resolve(result);
        return;
      }

      const component = prefetchComponents[index];
      index++;

      // Загружаем компонент
      component()
        .then(() => {
          result.loaded++;
          // Планируем загрузку следующего компонента когда браузер будет свободен
          if ("requestIdleCallback" in globalThis) {
            requestIdleCallback(() => loadNext(), { timeout: 2000 });
          } else {
            // Fallback для браузеров без requestIdleCallback
            setTimeout(loadNext, 100);
          }
        })
        .catch(error => {
          result.errors.push(error);
          console.warn("Failed to prefetch component:", error);
          // Продолжаем загрузку следующего компонента даже при ошибке
          if ("requestIdleCallback" in globalThis) {
            requestIdleCallback(() => loadNext(), { timeout: 2000 });
          } else {
            setTimeout(loadNext, 100);
          }
        });
    };

    // Начинаем загрузку с небольшой задержкой после первого рендера
    if ("requestIdleCallback" in globalThis) {
      requestIdleCallback(() => loadNext(), { timeout: 2000 });
    } else {
      setTimeout(loadNext, 100);
    }
  });
};

/**
 * Регистрация компонентов для предзагрузки
 */
export const registerPrefetchComponents = (
  components: LazyComponentLoader[]
): void => {
  prefetchComponents.push(...components);
};

/**
 * Регистрация критичных компонентов
 */
export const registerCriticalComponents = (
  components: LazyComponentLoader[]
): void => {
  criticalComponents.push(...components);
};
