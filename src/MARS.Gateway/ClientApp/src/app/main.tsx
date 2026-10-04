import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { ErrorBoundary } from "@/shared/components/ErrorBoundary/ErrorBoundary";
import { ToastModalProvider } from "@/shared/Utils/index.ts";

import App from "./App.tsx";

// Граница смонтирована здесь, а не написана «на будущее»: класс существовал, но
// импортов не имел ни одного, при том что три комментария в разборе payload и в
// сторе оверлея ссылались на неё как на работающую защиту. Падение в рендере
// одного экрана — а такое случалось, например, на `undefined.displayName` в луче
// MikuMikuBeam — уносило весь документ, то есть в OBS был белый экран.
createRoot(document.querySelector("#root")!).render(
  <StrictMode>
    <ErrorBoundary>
      <ToastModalProvider>
        <App />
      </ToastModalProvider>
    </ErrorBoundary>
  </StrictMode>
);
