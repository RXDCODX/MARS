import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import ActionButtons from "./ActionButtons";

const meta: Meta<typeof ActionButtons> = {
  title: "Stream Components/Scoreboard/AdminPanel/ActionButtons",
  component: ActionButtons,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Кнопки действий для управления скорбордом: смена имен игроков, смена позиций и сброс настроек.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    onSwapNames: { action: "swap names clicked" },
    onSwapPlayers: { action: "swap players clicked" },
    onReset: { action: "reset clicked" },
  },
  decorators: [
    Story => (
      <div
        style={{
          width: "300px",
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

    // Проверяем наличие кнопок действий
    const buttons = canvasElement.querySelectorAll("button");
    expect(buttons.length).toBeGreaterThan(0);
  },
};

export const AllButtons: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Все кнопки действий доступны.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие всех трех кнопок
    const buttons = canvasElement.querySelectorAll("button");
    expect(buttons.length).toBe(3);
  },
};

export const SwapNamesButton: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Кнопка для смены имен игроков.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие кнопки смены имен
    const swapNamesButton = canvasElement.querySelector(
      'button[title*="names"]'
    );
    expect(swapNamesButton).toBeInTheDocument();
  },
};

export const SwapPlayersButton: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Кнопка для смены позиций игроков.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие кнопки смены игроков
    const swapPlayersButton = canvasElement.querySelector(
      'button[title*="players"]'
    );
    expect(swapPlayersButton).toBeInTheDocument();
  },
};

export const ResetButton: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Кнопка для сброса всех настроек.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие кнопки сброса
    const resetButton = canvasElement.querySelector('button[title*="reset"]');
    expect(resetButton).toBeInTheDocument();
  },
};

export const ButtonStyles: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Различные стили кнопок действий.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие иконок в кнопках
    const icons = canvasElement.querySelectorAll("i");
    expect(icons.length).toBeGreaterThan(0);
  },
};

export const ResponsiveLayout: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Адаптивная компоновка кнопок действий.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие контейнера кнопок
    const container = canvasElement.querySelector('[class*="action-buttons"]');
    expect(container).toBeInTheDocument();
  },
};
