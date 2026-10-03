import { useEffect, useState } from "react";

import type { PrefetchStatus } from "../types/prefetch.types";
import { prefetchNonCriticalComponents } from "../utils/prefetchRoutes";

/**
 * Хук для предзагрузки некритичных компонентов в фоне
 * Запускается автоматически после монтирования компонента
 */
export const usePrefetchRoutes = (): PrefetchStatus => {
  const [status, setStatus] = useState<PrefetchStatus>({
    isLoading: false,
    loaded: 0,
    total: 0,
    errors: [],
  });

  useEffect(() => {
    setStatus(previous => ({ ...previous, isLoading: true }));

    prefetchNonCriticalComponents()
      .then(result => {
        setStatus({
          isLoading: false,
          loaded: result.loaded,
          total: result.total,
          errors: result.errors,
        });

        if (result.errors.length > 0) {
          console.warn(
            `Prefetch completed with ${result.errors.length} errors:`,
            result.errors
          );
        } else {
          console.log(
            `Successfully prefetched ${result.loaded}/${result.total} components`
          );
        }
      })
      .catch(error => {
        console.error("Prefetch failed:", error);
        setStatus(previous => ({
          ...previous,
          isLoading: false,
          errors: [...previous.errors, error],
        }));
      });
  }, []);

  return status;
};
