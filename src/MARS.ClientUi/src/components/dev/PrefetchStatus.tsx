import { usePrefetchRoutes } from "@/routes/hooks/usePrefetchRoutes";

import styles from "./PrefetchStatus.module.scss";

/**
 * Компонент для отображения статуса предзагрузки компонентов
 * Используется только в режиме разработки для отладки
 */
export const PrefetchStatus = () => {
  const { isLoading, loaded, total, errors } = usePrefetchRoutes();

  // Не показываем компонент в production
  if (import.meta.env.PROD) {
    return null;
  }

  const progress = total > 0 ? Math.round((loaded / total) * 100) : 0;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.title}>Prefetch Status</span>
        {isLoading && <span className={styles.spinner}>⏳</span>}
      </div>

      <div className={styles.progress}>
        <div className={styles.progressBar} style={{ width: `${progress}%` }} />
      </div>

      <div className={styles.stats}>
        <span>
          {loaded} / {total} loaded ({progress}%)
        </span>
        {errors.length > 0 && (
          <span className={styles.errors}>
            {errors.length} error{errors.length > 1 ? "s" : ""}
          </span>
        )}
      </div>

      {errors.length > 0 && (
        <details className={styles.errorDetails}>
          <summary>Errors</summary>
          <ul>
            {errors.map((error, index) => (
              <li key={index}>{error.message}</li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
};
