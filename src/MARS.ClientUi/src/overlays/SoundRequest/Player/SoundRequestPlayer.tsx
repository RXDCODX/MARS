import { useIsMobile } from "@/shared/hooks/useMediaQuery";

import { LiquidChrome } from "./Background";
import { SoundRequestPlayerDesktop } from "./Desktop/SoundRequestPlayerDesktop";
import { SoundRequestPlayerMobile } from "./Mobile/SoundRequestPlayerMobile";
import { SoundRequestPlayerInitializer } from "./SoundRequestPlayerInitializer";

/**
 * Адаптивный плеер для управления SoundRequest
 * Автоматически определяет разрешение экрана и рендерит соответствующий компонент
 * Инициализирует SignalR подключение и регистрирует actions в store
 */
export function SoundRequestPlayer() {
  const isMobile = useIsMobile();

  return (
    <>
      <SoundRequestPlayerInitializer>
        {isMobile ? (
          <SoundRequestPlayerMobile />
        ) : (
          <SoundRequestPlayerDesktop />
        )}
      </SoundRequestPlayerInitializer>
      <LiquidChrome key={1} />
    </>
  );
}
