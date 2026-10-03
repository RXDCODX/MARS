/**
 * Глобалы, которые объявляют сторонние скрипты, а не проект.
 *
 * Обе библиотеки существуют только на странице: `YT` появляется после загрузки
 * iframe_api, `webkitAudioContext` — префикс Safari, который до сих пор нужен для
 * старых версий. TypeScript о таком не знает, и без объявления каждое обращение
 * падало с «Element implicitly has an 'any' type because type 'typeof
 * globalThis' has no index signature» — восемь ошибок из-за одного пропущенного
 * шага.
 *
 * Объявлены через ar, а не const: только ar становится свойством
 * глобального объекта, и только тогда работает обращение globalThis.YT.
 * При const имя доступно как голый идентификатор, и код, который пишет
 * именно globalThis.X, получал TS2339.
 *
 * Объявления намеренно минимальны: описывается ровно то, чем пользуется проект.
 * Полное описание YouTube IFrame API в проект не тащится — оно большое, а
 * используется тут один конструктор плеера и один обработчик готовности.
 */

interface YoutubePlayerOptions {
  height: string | number;
  width: string | number;
  videoId: string;
  playerVars?: Record<string, string | number>;
  events?: Record<string, (event: { target: YoutubePlayer }) => void>;
}

interface YoutubePlayer {
  playVideo(): void;
  stopVideo(): void;
  setVolume(volume: number): void;
  mute(): void;
  unMute(): void;
  destroy(): void;
  getPlayerState(): number;
  getCurrentTime(): number;
  seekTo(seconds: number, allowSeekAhead: boolean): void;
}

interface YoutubePlayerConstructor {
  new (
    element: HTMLElement | string,
    options: YoutubePlayerOptions
  ): YoutubePlayer;
  PlayerState: {
    UNSTARTED: -1;
    ENDED: 0;
    PLAYING: 1;
    PAUSED: 2;
    BUFFERING: 3;
    CUED: 5;
  };
}

declare global {
  /** IFrame API YouTube. Появляется на странице после загрузки скрипта. */
  var YT: {
    Player: YoutubePlayerConstructor;
  };

  /** Вызывается библиотекой, когда API готов к работе. */
  var onYouTubeIframeAPIReady: () => void;

  /**
   * Префикс конструктора Web Audio в Safari.
   *
   * Устаревший, но в проекте ещё нужен: `AudioContext` там появился не везде, а
   * падать при первом же воспроизведении нельзя.
   */
  var webkitAudioContext: typeof AudioContext | undefined;
}

export {};
