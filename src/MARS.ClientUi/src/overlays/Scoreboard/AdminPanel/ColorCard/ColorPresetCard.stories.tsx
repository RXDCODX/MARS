import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import ColorPresetCard from "./ColorPresetCard";

const meta: Meta<typeof ColorPresetCard> = {
  title: "Stream Components/Scoreboard/AdminPanel/ColorPresetCard",
  component: ColorPresetCard,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Карточка для настройки цветовой схемы скорборда. Позволяет изменять все цвета элементов интерфейса.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    onColorChange: { action: "color changed" },
  },
  decorators: [
    Story => (
      <div
        style={{
          width: "600px",
          padding: "20px",
          background: "#f8f9fa",
        }}
      >
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие карточки цветов
    const card = canvasElement.querySelector('[class*="color-preset-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const BlueTheme: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Синяя цветовая схема для скорборда.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие цветовых полей
    const colorInputs = canvasElement.querySelectorAll('input[type="color"]');
    expect(colorInputs.length).toBeGreaterThan(0);
  },
};

export const RedTheme: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Красная цветовая схема для скорборда.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие пресетов
    const presetButtons = canvasElement.querySelectorAll("button");
    expect(presetButtons.length).toBeGreaterThan(0);
  },
};

export const GreenTheme: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Зеленая цветовая схема для скорборда.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие полей прозрачности
    const transparencyInputs = canvasElement.querySelectorAll(
      'input[type="range"]'
    );
    expect(transparencyInputs.length).toBeGreaterThan(0);
  },
};

export const PurpleTheme: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Фиолетовая цветовая схема для скорборда.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие лейблов цветов
    const labels = canvasElement.querySelectorAll("label");
    expect(labels.length).toBeGreaterThan(0);
  },
};

export const CustomColors: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Кастомная цветовая схема с индивидуальными настройками.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие всех элементов управления цветом
    const colorControls = canvasElement.querySelectorAll(
      '[class*="color-control"]'
    );
    expect(colorControls.length).toBeGreaterThan(0);
  },
};
