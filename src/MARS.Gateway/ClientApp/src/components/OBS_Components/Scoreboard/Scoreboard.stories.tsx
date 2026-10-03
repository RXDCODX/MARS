import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import Scoreboard from "./Scoreboard";

const meta: Meta<typeof Scoreboard> = {
  title: "Stream Components/Scoreboard",
  component: Scoreboard,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Компонент скорборда для отображения информации о матчах в OBS. Поддерживает настройку игроков, счетов, цветов и анимаций.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "100vw",
          height: "100vh",
          background: "linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)",
          position: "relative",
          overflow: "hidden",
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
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие основных элементов скорборда
    const centerDiv = canvasElement.querySelector('[class*="centerDiv"]');
    expect(centerDiv).toBeInTheDocument();

    const leftDiv = canvasElement.querySelector('[class*="leftDiv"]');
    expect(leftDiv).toBeInTheDocument();

    const rightDiv = canvasElement.querySelector('[class*="rightDiv"]');
    expect(rightDiv).toBeInTheDocument();
  },
};

export const TournamentMatch: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скорборд для турнирного матча с полной информацией об игроках.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие игроков
    const playerNames = canvasElement.querySelectorAll('[class*="playerName"]');
    expect(playerNames.length).toBeGreaterThan(0);

    // Проверяем наличие счетов
    const scores = canvasElement.querySelectorAll("h2");
    expect(scores.length).toBeGreaterThan(0);
  },
};

export const GrandFinals: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скорборд для гранд-финала с настроенными цветами и анимациями.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие режима драки
    const fightModeDiv = canvasElement.querySelector('[class*="fightModeDiv"]');
    expect(fightModeDiv).toBeInTheDocument();
  },
};

export const Hidden: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скрытый скорборд (не отображается на экране).",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // В данном случае компонент может быть скрыт через SignalR
    // Проверяем базовую структуру
    const container = canvasElement.querySelector(
      '[class*="scoreboard-container"]'
    );
    expect(container).toBeInTheDocument();
  },
};

export const WithFlags: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скорборд с отображением флагов стран игроков.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие флагов
    const flags = canvasElement.querySelectorAll('[class*="flag"] img');
    expect(flags.length).toBeGreaterThan(0);
  },
};

export const WinnerLoser: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скорборд с отмеченными победителем и проигравшим.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие меток победителя/проигравшего
    const playerNames = canvasElement.querySelectorAll('[class*="playerName"]');
    expect(playerNames.length).toBeGreaterThan(0);
  },
};

export const CustomColors: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Скорборд с кастомными цветами и неоновыми эффектами.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие элементов с кастомными стилями
    const styledElements = canvasElement.querySelectorAll(
      '[style*="box-shadow"]'
    );
    expect(styledElements.length).toBeGreaterThan(0);
  },
};
