import { useEffect, useRef } from "react";

/**
 * Хук для внедрения CSS стилей в head страницы
 * @param styles - CSS строка со стилями
 * @param id - Уникальный идентификатор для style элемента (опционально)
 * @returns void
 *
 * @example
 * useInjectStyles(`
 *   .my-class {
 *     color: red;
 *   }
 * `, 'my-custom-styles');
 */
export const useInjectStyles = (styles: string, id?: string): void => {
  const styleElementReference = useRef<HTMLStyleElement | null>(null);

  useEffect(() => {
    if (!styles) {
      return;
    }

    // Создаем style элемент
    const styleElement = document.createElement("style");
    styleElement.textContent = styles;

    if (id) {
      styleElement.id = id;

      // Удаляем существующий элемент с таким же id, если есть
      const existingElement = document.getElementById(id);
      if (existingElement) {
        existingElement.remove();
      }
    }

    // Добавляем в head
    document.head.append(styleElement);
    styleElementReference.current = styleElement;

    // Cleanup: удаляем элемент при размонтировании
    return () => {
      if (
        styleElementReference.current &&
        document.head.contains(styleElementReference.current)
      ) {
        styleElementReference.current.remove();
      }
    };
  }, [styles, id]);
};
