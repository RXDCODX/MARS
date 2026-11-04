import React from "react";

interface ThemeToggleProps {
  currentTheme: string;
  onThemeChange: (theme: string) => void;
}

export const ThemeToggle = ({
  currentTheme,
  onThemeChange,
}: ThemeToggleProps) => (
  <div
    style={{
      position: "fixed",
      top: "20px",
      right: "20px",
      zIndex: 9999,
      background: "rgba(0, 0, 0, 0.1)",
      border: "1px solid rgba(255, 255, 255, 0.2)",
      borderRadius: "8px",
      padding: "8px",
      backdropFilter: "blur(10px)",
      display: "flex",
      gap: "8px",
      alignItems: "center",
    }}
  >
    <span style={{ color: "white", fontSize: "12px" }}>Theme:</span>
    <button
      onClick={() => onThemeChange("light")}
      style={{
        background: currentTheme === "light" ? "#007bff" : "transparent",
        color: currentTheme === "light" ? "white" : "#ccc",
        border: "1px solid #ccc",
        borderRadius: "4px",
        padding: "4px 8px",
        cursor: "pointer",
        fontSize: "11px",
      }}
    >
      Light
    </button>
    <button
      onClick={() => onThemeChange("dark")}
      style={{
        background: currentTheme === "dark" ? "#007bff" : "transparent",
        color: currentTheme === "dark" ? "white" : "#ccc",
        border: "1px solid #ccc",
        borderRadius: "4px",
        padding: "4px 8px",
        cursor: "pointer",
        fontSize: "11px",
      }}
    >
      Dark
    </button>
  </div>
);
