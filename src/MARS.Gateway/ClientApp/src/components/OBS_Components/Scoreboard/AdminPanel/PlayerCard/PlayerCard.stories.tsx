import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import PlayerCard from "./PlayerCard";

const meta: Meta<typeof PlayerCard> = {
  title: "Stream Components/Scoreboard/AdminPanel/PlayerCard",
  component: PlayerCard,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Карточка игрока для панели администратора. Позволяет редактировать имя, спонсора, счет, тег, флаг и статус игрока.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "400px",
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

export const Player1: Story = {
  args: {
    playerIndex: 1,
    label: "Player 1",
    accent: "#0dcaf0",
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const Player2: Story = {
  args: {
    playerIndex: 2,
    label: "Player 2",
    accent: "#6610f2",
  },
  parameters: {
    docs: {
      description: {
        story: "Карточка второго игрока с фиолетовым акцентом.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const WithCustomAccent: Story = {
  args: {
    playerIndex: 1,
    label: "Custom Player",
    accent: "#ff6b35",
  },
  parameters: {
    docs: {
      description: {
        story: "Карточка игрока с кастомным оранжевым акцентом.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const LongLabel: Story = {
  args: {
    playerIndex: 1,
    label: "Very Long Player Label",
    accent: "#28a745",
  },
  parameters: {
    docs: {
      description: {
        story: "Карточка игрока с длинным названием.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const DarkAccent: Story = {
  args: {
    playerIndex: 2,
    label: "Dark Player",
    accent: "#343a40",
  },
  parameters: {
    docs: {
      description: {
        story: "Карточка игрока с темным акцентом.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const NeonAccent: Story = {
  args: {
    playerIndex: 1,
    label: "Neon Player",
    accent: "#00ff88",
  },
  parameters: {
    docs: {
      description: {
        story: "Карточка игрока с неоновым зеленым акцентом.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов
    const card = canvasElement.querySelector('[class*="player-card"]');
    expect(card).toBeInTheDocument();
  },
};
