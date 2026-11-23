/**
 * Компонент загрузки для Suspense при lazy loading OBS компонентов
 */
export const OBSLazyLoader = () => (
  <div
    style={{
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      height: "100vh",
      backgroundColor: "transparent",
    }}
  >
    <div style={{ color: "white", fontSize: "24px" }}>Загрузка...</div>
  </div>
);

/**
 * Компонент загрузки для Suspense при lazy loading страниц сайта
 */
export const PageLoader = () => (
  <div
    style={{
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      minHeight: "60vh",
      color: "var(--site-text-primary)",
    }}
  >
    <div style={{ fontSize: "20px" }}>Загрузка страницы...</div>
  </div>
);
