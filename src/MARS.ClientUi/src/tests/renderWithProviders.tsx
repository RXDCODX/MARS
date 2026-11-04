import { render } from "@testing-library/react";
import type { ReactElement, ReactNode } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { ThemeProvider } from "@/contexts/ThemeContext";
import {
  AntdStyleProvider,
  ThemeProvider as AntThemeProvider,
} from "@/shared/components/ui";
import { ToastModalProvider } from "@/shared/Utils/ToastModal/ToastModal";

/**
 * Провайдеры приложения для тестов.
 *
 * Стек зеркарит `main.tsx` и `App.tsx`: `ToastModalProvider` → `ThemeProvider` →
 * `AntdStyleProvider` → `AntThemeProvider`. Порядок важен и повторяет
 * приложение, иначе тест проверяет не то дерево, которое увидит пользователь.
 *
 * Раньше провайдеры повторяли по памяти и по неполноте: `Header` требует
 * `AntdStyleProvider`, `WelcomePage` и `useQueueActions` — `ToastModalProvider`,
 * а `useAntdStyle` бросает «должен использоваться внутри AntdStyleProvider»
 * прямо во время рендера. Ошибка приходит изнутри React и выглядит как
 * «тест сломан», хотя компонент исправен.
 *
 * Новый глобальный провайдер добавляется сюда, а не в каждый тест по
 * отдельности: тогда забыть его нельзя.
 */
export function Providers({ children }: { children: ReactNode }) {
  return (
    <ToastModalProvider>
      <ThemeProvider>
        <AntdStyleProvider>
          <AntThemeProvider>{children}</AntThemeProvider>
        </AntdStyleProvider>
      </ThemeProvider>
    </ToastModalProvider>
  );
}

/**
 * `render` из testing-library с провайдерами приложения.
 *
 * Аргументы и возвращаемое значение совпадают с обычным `render`, поэтому
 * подмена происходит заменой импорта, а не переписыванием теста.
 */
export function renderWithProviders(ui: ReactElement) {
  return render(ui, { wrapper: Providers });
}

/**
 * То же для серверного рендера в строку.
 *
 * Отдельная функция, потому что статический рендер в разы дешевле и им
 * проверяют разметку: `Header.test.tsx` и `WelcomePage.test.tsx` смотрят на
 * HTML, а не на поведение.
 */
export function renderToStaticMarkupWithProviders(ui: ReactElement): string {
  return renderToStaticMarkup(<Providers>{ui}</Providers>);
}
