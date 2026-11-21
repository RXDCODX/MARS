import { DesktopOutlined, MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";

import { useTheme } from "@/contexts/Theme";

interface ThemeToggleProperties {
  variant?: "default" | "admin";
  size?: "sm" | "md" | "lg";
}

const NEXT_LABEL: Record<"light" | "dark" | "system", string> = {
  light: "Тёмная",
  dark: "Системная",
  system: "Светлая",
};

const NEXT_TITLE: Record<"light" | "dark" | "system", string> = {
  light: "темную",
  dark: "системную",
  system: "светлую",
};

const ThemeToggle: React.FC<ThemeToggleProperties> = () => {
  const { theme, toggleTheme } = useTheme();

  const icon =
    theme === "light" ? (
      <MoonOutlined />
    ) : theme === "dark" ? (
      <DesktopOutlined />
    ) : (
      <SunOutlined />
    );

  return (
    <Button
      type="default"
      size="small"
      icon={icon}
      onClick={toggleTheme}
      title={`Переключить на ${NEXT_TITLE[theme]} тему`}
      data-testid="button-theme-toggle"
    >
      <span style={{ display: "inline", fontSize: 12 }}>
        {NEXT_LABEL[theme]}
      </span>
    </Button>
  );
};

export default ThemeToggle;
