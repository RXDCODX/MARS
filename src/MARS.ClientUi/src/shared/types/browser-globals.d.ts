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
  /** Ролик. Не обязателен: плеер создаётся и под плейлист. */
  videoId?: string;
  playerVars?: Record<string, string | number>;
  /**
   * Обработчики плеера. Событие несёт сам плеер и код состояния или
   * ошибки — без него обработчик onReady не смог бы загрузить плейлист.
   */
  events?: Record<string, (event: YoutubePlayerEvent) => void>;
}

/** Событие плеера: сам плеер плюс код состояния или ошибки. */
interface YoutubePlayerEvent {
  target: YoutubePlayer;
  data?: number | string;
}

interface YoutubePlayer {
  /** Загрузка плейлиста с позицией и временем старта. */
  loadPlaylist(options: {
    listType: string;
    list: string;
    index: number;
    startSeconds: number;
  }): void;
  /** Переход к следующему видео в плейлисте. */
  nextVideo(): void;
  /** Перемешивание плейлиста. */
  setShuffle(enabled: boolean): void;
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

/**
 * Состояния плеера.
 *
 * Объявлены отдельным именем, потому что обращаются и как
 * YT.PlayerState.ENDED, и через сам конструктор. Без общего объявления
 * каждое такое обращение падало с TS2339.
 */
interface YoutubePlayerState {
  UNSTARTED: -1;
  ENDED: 0;
  PLAYING: 1;
  PAUSED: 2;
  BUFFERING: 3;
  CUED: 5;
}

interface YoutubePlayerConstructor {
  new (
    element: HTMLElement | string,
    options: YoutubePlayerOptions
  ): YoutubePlayer;
  PlayerState: YoutubePlayerState;
}

declare global {
  /** IFrame API YouTube. Появляется на странице после загрузки скрипта. */
  var YT: {
    Player: YoutubePlayerConstructor;
    PlayerState: YoutubePlayerState;
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
