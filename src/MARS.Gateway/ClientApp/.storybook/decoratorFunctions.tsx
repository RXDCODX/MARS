// eslint-disable-next-line no-restricted-imports
import React, { type ComponentType } from "react";

// Декоратор для полного экрана с градиентным фоном
export const withFullScreenGradient = (Story: ComponentType) => (
  <div
    style={{
      width: "100vw",
      height: "100vh",
      background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
      position: "relative",
      overflow: "hidden",
    }}
  >
    <Story />
  </div>
);

// Декоратор для темного фона
export const withDarkBackground = (Story: ComponentType) => (
  <div
    style={{
      width: "100vw",
      height: "100vh",
      background: "#000",
      position: "relative",
      overflow: "hidden",
    }}
  >
    <Story />
  </div>
);

// Декоратор для центрированного контента
export const withCentered = (Story: ComponentType) => (
  <div
    style={{
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      minHeight: "100vh",
      padding: "20px",
    }}
  >
    <Story />
  </div>
);
