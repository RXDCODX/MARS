import type { Meta, StoryObj } from "@storybook/react-vite";
import { BrowserRouter } from "react-router-dom";
import { expect } from "storybook/test";

import AdminPanel from "./AdminPanel";

const meta: Meta<typeof AdminPanel> = {
  title: "Stream Components/Scoreboard/AdminPanel",
  component: AdminPanel,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Панель администратора для управления скорбордом. Позволяет настраивать игроков, цвета, видимость и мета-информацию.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <BrowserRouter>
        <div
          style={{
            width: "100vw",
            height: "100vh",
            background: "#f8f9fa",
            position: "relative",
            overflow: "auto",
          }}
        >
          <Story />
        </div>
      </BrowserRouter>
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

    // Проверяем наличие основных панелей
    const adminPanel = canvasElement.querySelector('[class*="adminPanel"]');
    expect(adminPanel).toBeInTheDocument();
  },
};

export const Desktop: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Панель администратора в десктопном режиме с полным функционалом.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие карточек игроков
    const playerCards = canvasElement.querySelectorAll(
      '[class*="player-card"]'
    );
    expect(playerCards.length).toBeGreaterThan(0);
  },
};

export const Mobile: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Панель администратора в мобильном режиме (автоматический редирект на /admin).",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // В мобильном режиме должен произойти редирект
    // Проверяем базовую структуру
    const container = canvasElement.querySelector('[class*="admin-panel"]');
    expect(container).toBeInTheDocument();
  },
};

export const WithCustomData: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Панель с предустановленными данными игроков и турнира.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие элементов управления
    const actionButtons = canvasElement.querySelectorAll("button");
    expect(actionButtons.length).toBeGreaterThan(0);
  },
};

export const ColorCustomization: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Панель с акцентом на настройку цветов скорборда.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие цветовых настроек
    const colorInputs = canvasElement.querySelectorAll('input[type="color"]');
    expect(colorInputs.length).toBeGreaterThan(0);
  },
};

export const PlayerManagement: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Панель с акцентом на управление данными игроков.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие полей ввода для игроков
    const textInputs = canvasElement.querySelectorAll('input[type="text"]');
    expect(textInputs.length).toBeGreaterThan(0);
  },
};
