import { useEffect, useLayoutEffect } from "react";

import { useTelegramusHubStore } from "@/shared/stores/telegramusHubStore";

// Компонент-обертка для OBS компонентов
export const OBSComponentWrapper = ({
  children,
}: {
  children: React.ReactNode;
}) => {
  useLayoutEffect(() => {
    const previousBg = document.body.style.getPropertyValue("background-color");
    const previousImportant =
      document.body.style.getPropertyPriority("background-color");
    document.body.style.setProperty(
      "background-color",
      "transparent",
      "important"
    );

    return () => {
      document.body.style.setProperty(
        "background-color",
        previousBg,
        previousImportant || undefined
      );
    };
  }, []);

  // Раньше здесь стоял TelegramusHubSignalRHubWrapper из react-signalr: он
  // оборачивал дерево в контекст с соединением. Теперь соединением владеет
  // HubAdapter, а компоненты подписываются хуком useOverlayEvent через реестр,
  // поэтому обход дерева не нужен — нужен только старт и остановка.
  //
  // Строгая проверка отсутствует намеренно: компонент может смонтироваться
  // раньше соединения, и его подписки доживутся подключения через реестр.
  const start = useTelegramusHubStore(state => state.start);
  const stop = useTelegramusHubStore(state => state.stop);

  useEffect(() => {
    void start();

    return () => {
      void stop();
    };
  }, [start, stop]);

  return (
    <div className="obs-component" data-testid="obs-component-wrapper">
      {children}
    </div>
  );
};
