import { memo, ReactNode } from "react";

import { useSoundRequestPlayer } from "./hooks";

interface Properties {
  children: ReactNode;
}

/**
 * Внутренний хук для инициализации плеера
 * Не рендерит ничего, только вызывает хук
 */
function PlayerInitializer() {
  useSoundRequestPlayer();
  return null;
}

/**
 * Компонент-инициализатор для Sound Request плеера
 * Инициализирует SignalR, регистрирует actions в store и синхронизирует данные
 * Используется как wrapper для всех версий плеера (Desktop/Mobile)
 */
function SoundRequestPlayerInitializerComponent({ children }: Properties) {
  return (
    <>
      <PlayerInitializer />
      {children}
    </>
  );
}

export const SoundRequestPlayerInitializer = memo(
  SoundRequestPlayerInitializerComponent
);
