import { useLayoutEffect } from "react";

import { useOverlayHubLifecycle } from "@/shared/realtime/useOverlayHubLifecycle";

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
  //
  // Жизненный цикл общий для всех экранов, потому что раньше его дублировали ещё
  // четыре компонента, и отказ подключения давал четыре `Unhandled promise
  // rejection` на пустом экране.
  useOverlayHubLifecycle();

  return (
    <div className="obs-component" data-testid="obs-component-wrapper">
      {children}
    </div>
  );
};
