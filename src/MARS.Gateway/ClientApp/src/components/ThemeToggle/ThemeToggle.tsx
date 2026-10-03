import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";

import { useTheme } from "@/contexts/Theme";

interface ThemeToggleProperties {
  variant?: "default" | "admin";
  size?: "sm" | "md" | "lg";
}

const ThemeToggle: React.FC<ThemeToggleProperties> = ({
  variant = "default",
}) => {
  const { theme, toggleTheme } = useTheme();

  return (
    <Button
      type="default"
      size="small"
      icon={theme === "light" ? <MoonOutlined /> : <SunOutlined />}
      onClick={toggleTheme}
      title={`Переключить на ${theme === "light" ? "темную" : "светлую"} тему`}
      data-testid="button-theme-toggle"
    >
      <span style={{ display: "inline", fontSize: 12 }}>
        {theme === "light" ? "Тёмная" : "Светлая"}
      </span>
    </Button>
  );
};

export default ThemeToggle;
