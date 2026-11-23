import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import WaifuAlerts from "./WaifuAlerts";

const meta: Meta<typeof WaifuAlerts> = {
  title: "Stream Components/WaifuAlerts",
  component: WaifuAlerts,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component: "Система алертов для вайфу-рулетки с анимациями и рулеткой.",
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
          background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
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
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет активных вайфу-алертов изначально
    const waifuAlerts = canvasElement.querySelectorAll('[class*="baza"]');
    expect(waifuAlerts.length).toBe(0);

    // Проверяем, что нет рулетки изначально
    const roulette = canvasElement.querySelectorAll('[class*="roulette"]');
    expect(roulette.length).toBe(0);
  },
};

export const Empty: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Пустая система вайфу-алертов без активных событий.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет вайфу-алертов
    const waifuAlerts = canvasElement.querySelectorAll('[class*="baza"]');
    expect(waifuAlerts.length).toBe(0);

    // Проверяем, что нет рулетки
    const roulette = canvasElement.querySelectorAll('[class*="roulette"]');
    expect(roulette.length).toBe(0);

    // Проверяем, что нет конфетти
    const confetti = canvasElement.querySelectorAll("canvas");
    expect(confetti.length).toBe(0);
  },
};
