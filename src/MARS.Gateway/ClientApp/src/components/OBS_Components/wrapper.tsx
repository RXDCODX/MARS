import { useLayoutEffect } from "react";

import { TelegramusHubSignalRHubWrapper } from "@/shared/api";

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

  return (
    <TelegramusHubSignalRHubWrapper>
      <div className="obs-component" data-testid="obs-component-wrapper">
        {children}
      </div>
    </TelegramusHubSignalRHubWrapper>
  );
};
