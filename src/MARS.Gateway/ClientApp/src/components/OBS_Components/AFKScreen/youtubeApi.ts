/**
 * Готовность IFrame API YouTube.
 *
 * Отдельный модуль, потому что проверять готовность приходится до создания
 * плеера, и проверка эта — не «есть ли объект», а «есть ли конструктор».
 * Разница видна только в CI: библиотека создаёт `window.YT` раньше, чем
 * появляется `Player`, и старая проверка `if (!globalThis.YT)` считала такой
 * `YT` готовым. Дальше `new globalThis.YT.Player(...)` падал, исключение из
 * эффекта ловила граница ошибок, и `/afkscreen` показывал заглушку.
 *
 * Модуль ничего не бросает: неготовность — это «unavailable», по которому
 * компонент показывает свой экран с кнопкой повтора.
 */

const IFRAME_API_SRC = "https://www.youtube.com/iframe_api";

/** Сколько ждать обещанной готовности, прежде чем признать её не случившейся. */
const DEFAULT_READY_TIMEOUT_MS = 10_000;

/** Готов ли API к созданию плеера. */
export type YouTubeApiReadiness = "ready" | "unavailable";

/**
 * Готовность API.
 *
 * Именно конструктор, а не объект: `YT` без `Player` — это ещё не API,
 * и `new YT.Player` на таком объекте бросает «is not a constructor».
 */
export const isYouTubeApiReady = (): boolean =>
  typeof globalThis.YT?.Player === "function";

/** Вставляет скрипт библиотеки, если он ещё не подключён. */
const ensureScriptInjected = (): void => {
  if (document.querySelector(`script[src="${IFRAME_API_SRC}"]`) !== null) {
    return;
  }

  // Опора искалась не ради красоты: если скриптов на странице нет, вставлять
  // нечего, но подключение всё равно нужно. Поэтому тег добавляется в `head`
  // безусловно, а не перед первым найденным скриптом — обращение к `parentNode`
  // отсутствующего элемента роняло экран.
  const tag = document.createElement("script");
  tag.src = IFRAME_API_SRC;
  document.head.appendChild(tag);
};

/**
 * Дожидается готовности API.
 *
 * Обработчик `onYouTubeIframeAPIReady` — единственный слот, а не рассылка: его
 * перезаписывают, поэтому прежний обработчик вызывается, а не теряется. Иначе
 * экран, смонтированный вторым, тихо украл бы готовность у первого, и тот
 * висел бы вечно.
 *
 * Возвращает `unavailable` вместо исключения по таймауту: загруженный скрипт,
 * который так и не позвал обработчик, — это тоже состояние экрана, а не падение.
 */
export const ensureYouTubeApiAsync = async (
  timeoutMs: number = DEFAULT_READY_TIMEOUT_MS
): Promise<YouTubeApiReadiness> => {
  if (isYouTubeApiReady()) {
    return "ready";
  }

  await new Promise<void>(resolve => {
    let settled = false;

    const finish = () => {
      if (settled) {
        return;
      }

      settled = true;
      globalThis.onYouTubeIframeAPIReady = previousHandler;
      resolve();
    };

    const previousHandler = globalThis.onYouTubeIframeAPIReady;
    globalThis.onYouTubeIframeAPIReady = () => {
      previousHandler?.();
      finish();
    };

    ensureScriptInjected();

    if (isYouTubeApiReady()) {
      finish();
      return;
    }

    window.setTimeout(finish, timeoutMs);
  });

  return isYouTubeApiReady() ? "ready" : "unavailable";
};
