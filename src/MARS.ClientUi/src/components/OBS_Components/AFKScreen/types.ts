export interface AFKScreenProps {
  // Основные настройки
  videoId?: string;
  playlistId?: string;

  // Управление плеером
  autoplay?: boolean;
  controls?: boolean;
  loop?: boolean;
  muted?: boolean;

  // Размеры
  width?: string | number;
  height?: string | number;

  // Стилизация
  className?: string;
}

export interface YouTubePlayerInstance {
  playVideo(): Promise<void>;
  pauseVideo(): Promise<void>;
  stopVideo(): Promise<void>;
  mute(): Promise<void>;
  unMute(): Promise<void>;
  isMuted(): Promise<boolean>;
  destroy(): Promise<void>;
  on(event: string, callback: (event: any) => void): void;
  getPlayerState(): Promise<number>;
  getCurrentTime(): Promise<number>;
  getDuration(): Promise<number>;
  seekTo(seconds: number, allowSeekAhead?: boolean): Promise<void>;
  loadVideoById(videoId: string, startSeconds?: number): Promise<void>;
  cueVideoById(videoId: string, startSeconds?: number): Promise<void>;
}

export interface PlayerEvent {
  type: "ready" | "stateChange" | "error";
  data?: any;
  timestamp: number;
}
