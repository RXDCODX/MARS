/**
 * Константы таймингов анимаций для ChatVertical компонента
 */

// Время анимаций в миллисекундах
export const ANIMATION_TIMINGS = {
  // CSS slideInLeft анимация (появление слева направо)
  CSS_SLIDE_IN_LEFT: 10_000, // 1 секунда

  // CSS slideOutRight анимация (исчезновение справа налево)
  CSS_SLIDE_OUT_RIGHT: 1200, // 1 секунда

  // Короткий сдвиг существующих сообщений вверх перед появлением нового
  SHIFT_UP_DURATION: 500, // 0.3 секунды

  // Общее время для завершения всех анимаций
  TOTAL_ANIMATION_TIME: 2000, // 1.2 секунды

  // Дополнительный буфер для гарантии завершения
  SAFETY_BUFFER: 200,

  // Время жизни сообщения на экране
  MESSAGE_LIFETIME: {
    DEV: 10 * 60 * 1000, // 10 минут для разработки
    PRODUCTION: 30 * 1000, // 30 секунд для production
  },
} as const;

// Вычисляемое время ожидания для скролла
export const SCROLL_TIMEOUT =
  ANIMATION_TIMINGS.SHIFT_UP_DURATION +
  Math.max(
    ANIMATION_TIMINGS.CSS_SLIDE_IN_LEFT,
    ANIMATION_TIMINGS.CSS_SLIDE_OUT_RIGHT
  ) +
  ANIMATION_TIMINGS.SAFETY_BUFFER;

// Параметры анимаций Framer Motion (можно настраивать)
export const MOTION = {
  SPRING: {
    STIFFNESS: 160,
    DAMPING: 28,
    MASS: 1,
  },
  ENTRY: {
    X_OFFSET: 100, // пикселей (fallback, если проценты не подойдут)
    OFFSCREEN_PERCENT: 120, // старт за пределами экрана по оси X
    DURATION_MS: 1500, // длительность въезда
    DELAY_MS: 180, // задержка перед въездом нового сообщения, чтобы старые успели подъехать
  },
  EXIT: {
    X_OFFSET: 120, // пикселей (fallback)
    OFFSCREEN_PERCENT: 120, // уехать за экран
    DURATION_MS: 1000, // длительность выезда
  },
  LAYOUT: {
    DURATION_MS: 260, // длительность layout-анимации для «переезда»
  },
  OPACITY_DURATION_MS: 220,
} as const;

// Настройки CSS анимаций
export const CSS_ANIMATION_CONFIG = {
  slideInLeft: {
    duration: "1s",
    className: "slideInLeft",
  },
  slideOutRight: {
    duration: "1s",
    className: "slideOutRight",
  },
  shiftUp: {
    duration: "0.3s",
    className: "slideInUp", // используем готовую анимацию подъема
  },
} as const;

// Настройки скролла
export const SCROLL_CONFIG = {
  behavior: "smooth" as ScrollBehavior,
  block: "end" as ScrollLogicalPosition,
  inline: "nearest" as ScrollLogicalPosition,
} as const;
