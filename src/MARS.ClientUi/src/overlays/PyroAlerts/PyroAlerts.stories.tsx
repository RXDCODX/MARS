import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import PyroAlerts from "./PyroAlerts";

const meta: Meta<typeof PyroAlerts> = {
  title: "Stream Components/PyroAlerts",
  component: PyroAlerts,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Система алертов для стрима с поддержкой различных типов медиа (изображения, видео, аудио).",
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
          background: "#000",
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

    // Проверяем, что нет активных алертов изначально
    const alerts = canvasElement.querySelectorAll('[class*="media"]');
    expect(alerts.length).toBe(0);
  },
};

export const Empty: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Пустая система алертов без активных уведомлений.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет алертов
    const alerts = canvasElement.querySelectorAll('[class*="media"]');
    expect(alerts.length).toBe(0);

    // Проверяем, что нет высокоприоритетных алертов
    const highPriorityAlerts = canvasElement.querySelectorAll(
      '[class*="highPriority"]'
    );
    expect(highPriorityAlerts.length).toBe(0);
  },
};

export const LongText: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Демонстрация переноса длинного текста в алерте. Текст не должен выходить за границы медиа.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();
  },
};
