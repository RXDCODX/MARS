import { useEffect, useState } from "react";

/**
 * Хук для определения размера экрана и адаптивности
 * @param query - CSS медиа-запрос (например, '(max-width: 768px)')
 * @returns boolean - true если запрос соответствует текущему размеру экрана
 */
export const useMediaQuery = (query: string): boolean => {
  const [matches, setMatches] = useState(false);

  useEffect(() => {
    const media = globalThis.matchMedia(query);

    if (media.matches !== matches) {
      setMatches(media.matches);
    }

    const listener = () => setMatches(media.matches);
    media.addEventListener("change", listener);

    return () => media.removeEventListener("change", listener);
  }, [matches, query]);

  return matches;
};

/**
 * Хук для определения мобильного разрешения (до 768px)
 */
export const useIsMobile = () => useMediaQuery("(max-width: 768px)");

/**
 * Хук для определения планшетного разрешения (от 768px до 1024px)
 */
export const useIsTablet = () =>
  useMediaQuery("(min-width: 768px) and (max-width: 1024px)");

/**
 * Хук для определения десктопного разрешения (от 1024px)
 */
export const useIsDesktop = () => useMediaQuery("(min-width: 1024px)");
