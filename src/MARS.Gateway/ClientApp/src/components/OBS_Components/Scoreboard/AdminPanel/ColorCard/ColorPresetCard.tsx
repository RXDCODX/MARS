import { Button, Card, Flex, Typography } from "antd";
import { Palette } from "lucide-react";
import { useEffect, useState } from "react";

import { useSiteColors } from "../../../../../shared/Utils/useSiteColors";
import { useColor, useColorActions } from "../store/scoreboardStore";
import { ColorPreset, defaultPreset } from "../types";
import ColorPickerWithTransparency from "./ColorPickerWithTransparency";

const ColorPresetCard: React.FC = () => {
  const colors = useSiteColors();
  const scoreboardColors = useColor();
  const { handleColorChange } = useColorActions();
  const [customColors, setCustomColors] = useState(
    scoreboardColors || defaultPreset
  );

  useEffect(() => {
    if (scoreboardColors) {
      setCustomColors(scoreboardColors);
    }
  }, [scoreboardColors]);

  const colorPresets = [
    defaultPreset,
    {
      name: "Classic Blue",
      mainColor: "#0DCAF0",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#0DCAF0",
      fightModeColor: "#0DCAF0",
      scoreColor: "#FFFFFF",
      backgroundColor: "#0F1218",
      borderColor: "#0DCAF0",
    },
    {
      name: "Fire Red",
      mainColor: "#DC3545",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#FFC2C7",
      fightModeColor: "#FF6B74",
      scoreColor: "#FFFFFF",
      backgroundColor: "#15161A",
      borderColor: "#DC3545",
    },
    {
      name: "Forest Green",
      mainColor: "#198754",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#A8E6C5",
      fightModeColor: "#20C997",
      scoreColor: "#FFFFFF",
      backgroundColor: "#121517",
      borderColor: "#198754",
    },
    {
      name: "Purple Night",
      mainColor: "#6F42C1",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#BDA5F5",
      fightModeColor: "#9A6CFF",
      scoreColor: "#FFFFFF",
      backgroundColor: "#121021",
      borderColor: "#6F42C1",
    },
    {
      name: "Golden",
      mainColor: "#FFC107",
      playerNamesColor: "#1B1E24",
      tournamentTitleColor: "#FFD966",
      fightModeColor: "#FFB000",
      scoreColor: "#1B1E24",
      backgroundColor: "#23272F",
      borderColor: "#FFC107",
    },
    {
      name: "Neon Mint",
      mainColor: "#00FF88",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#66FFD1",
      fightModeColor: "#00E676",
      scoreColor: "#FFFFFF",
      backgroundColor: "#000000",
      borderColor: "#00FF88",
    },
    {
      name: "Neon Blue",
      mainColor: "#08F",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#66B3FF",
      fightModeColor: "#00A3FF",
      scoreColor: "#FFFFFF",
      backgroundColor: "#000000",
      borderColor: "#08F",
    },
    {
      name: "Dracula",
      mainColor: "#BD93F9",
      playerNamesColor: "#F8F8F2",
      tournamentTitleColor: "#50FA7B",
      fightModeColor: "#FF79C6",
      scoreColor: "#F8F8F2",
      backgroundColor: "#282A36",
      borderColor: "#6272A4",
    },
    {
      name: "Nord",
      mainColor: "#88C0D0",
      playerNamesColor: "#ECEFF4",
      tournamentTitleColor: "#A3BE8C",
      fightModeColor: "#81A1C1",
      scoreColor: "#ECEFF4",
      backgroundColor: "#2E3440",
      borderColor: "#5E81AC",
    },
    {
      name: "Gruvbox",
      mainColor: "#FABD2F",
      playerNamesColor: "#FBF1C7",
      tournamentTitleColor: "#B8BB26",
      fightModeColor: "#83A598",
      scoreColor: "#FBF1C7",
      backgroundColor: "#282828",
      borderColor: "#D79921",
    },
    {
      name: "Solarized Dark",
      mainColor: "#268BD2",
      playerNamesColor: "#EEE8D5",
      tournamentTitleColor: "#2AA198",
      fightModeColor: "#B58900",
      scoreColor: "#EEE8D5",
      backgroundColor: "#002B36",
      borderColor: "#93A1A1",
    },
    {
      name: "Cyberpunk",
      mainColor: "#FF007A",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#00E5FF",
      fightModeColor: "#FFE600",
      scoreColor: "#FFFFFF",
      backgroundColor: "#0A0B0F",
      borderColor: "#FF007A",
    },
    {
      name: "Oceanic",
      mainColor: "#00B3B3",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#4DD0E1",
      fightModeColor: "#26C6DA",
      scoreColor: "#FFFFFF",
      backgroundColor: "#0D1B1E",
      borderColor: "#00B3B3",
    },
    {
      name: "Sunset",
      mainColor: "#FF7A59",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#FFD166",
      fightModeColor: "#EF476F",
      scoreColor: "#FFFFFF",
      backgroundColor: "#1C0F13",
      borderColor: "#FF7A59",
    },
    {
      name: "Lime on Slate",
      mainColor: "#C3E88D",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#89DDFF",
      fightModeColor: "#82AAFF",
      scoreColor: "#FFFFFF",
      backgroundColor: "#101317",
      borderColor: "#C3E88D",
    },
    {
      name: "High Contrast",
      mainColor: "#FFFFFF",
      playerNamesColor: "#FFFFFF",
      tournamentTitleColor: "#FFD400",
      fightModeColor: "#00D1FF",
      scoreColor: "#FFFFFF",
      backgroundColor: "#000000",
      borderColor: "#FFFFFF",
    },
  ];

  const applyPreset = (preset: ColorPreset) => {
    setCustomColors(preset);
    handleColorChange(preset);
  };

  const handleCustomColorChange = (
    field: keyof typeof customColors,
    value: string
  ) => {
    const updatedColors = { ...customColors, [field]: value };
    setCustomColors(updatedColors);
    handleColorChange(updatedColors);
  };

  const colorFields: Array<{ key: keyof typeof customColors; label: string }> =
    [
      { key: "mainColor", label: "Основной цвет" },
      { key: "playerNamesColor", label: "Цвет имен игроков" },
      { key: "tournamentTitleColor", label: "Цвет заголовка турнира" },
      { key: "fightModeColor", label: "Цвет режима боя" },
      { key: "scoreColor", label: "Цвет счета" },
      { key: "backgroundColor", label: "Цвет фона" },
      { key: "borderColor", label: "Цвет границ" },
    ];

  return (
    <Card
      style={{
        backgroundColor: colors.background.card,
        borderRadius: 18,
        border: `2px solid ${colors.border.accent}`,
        marginBottom: 16,
        boxShadow: "var(--site-shadow-heavy)",
      }}
    >
      <Flex vertical align="center" style={{ marginBottom: 16, gap: 8 }}>
        <Palette color={colors.text.accent} size={24} />
        <Typography.Text
          strong
          style={{
            color: colors.text.accent,
            letterSpacing: 1,
            fontSize: 16,
            textTransform: "uppercase",
          }}
        >
          Color Presets
        </Typography.Text>
      </Flex>

      <div style={{ marginBottom: 16 }}>
        <Typography.Text
          strong
          style={{
            color: colors.text.primary,
            fontSize: 14,
            display: "block",
            marginBottom: 12,
          }}
        >
          Быстрые пресеты
        </Typography.Text>
        <Flex gap={8} wrap="wrap">
          {colorPresets.map((preset, index) => (
            <Button
              key={index}
              size="small"
              onClick={() => applyPreset(preset)}
              style={{
                flex: "0 0 calc(25% - 6px)",
                fontSize: 14,
                borderColor: preset.mainColor,
                color: preset.mainColor,
                minHeight: 40,
                fontWeight: 700,
              }}
            >
              {preset.name || "Default"}
            </Button>
          ))}
        </Flex>
      </div>

      <div>
        <Typography.Text
          strong
          style={{
            color: colors.text.primary,
            fontSize: 14,
            display: "block",
            marginBottom: 12,
          }}
        >
          Кастомные цвета
        </Typography.Text>
        <Flex gap={12} wrap="wrap">
          {colorFields.map(({ key, label }) => (
            <div key={key} style={{ flex: "0 0 calc(50% - 6px)" }}>
              <Typography.Text
                strong
                style={{ fontSize: 12, display: "block", marginBottom: 4 }}
              >
                {label}
              </Typography.Text>
              <ColorPickerWithTransparency
                value={(customColors[key] as string) || "#ffffff"}
                onChange={color => handleCustomColorChange(key, color)}
              />
            </div>
          ))}
        </Flex>
      </div>

      <Flex justify="center" style={{ marginTop: 16 }}>
        <Button
          size="small"
          onClick={() => applyPreset(defaultPreset)}
          style={{
            borderColor: colors.border.secondary,
            color: colors.text.secondary,
            fontWeight: 700,
          }}
        >
          Сбросить к умолчанию
        </Button>
      </Flex>
    </Card>
  );
};

export default ColorPresetCard;
