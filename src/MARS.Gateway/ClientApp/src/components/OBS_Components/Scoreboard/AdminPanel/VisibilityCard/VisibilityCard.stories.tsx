import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import VisibilityCard from "./VisibilityCard";

const meta: Meta<typeof VisibilityCard> = {
  title: "Stream Components/Scoreboard/AdminPanel/VisibilityCard",
  component: VisibilityCard,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Карточка для управления видимостью скорборда и настройки времени анимации.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    onVisibilityChange: { action: "visibility changed" },
    onAnimationDurationChange: { action: "animation duration changed" },
  },
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

export const Default: Story = {
  args: {
    isVisible: true,
    animationDuration: 800,
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие карточки видимости
    const card = canvasElement.querySelector('[class*="visibility-card"]');
    expect(card).toBeInTheDocument();
  },
};

export const Visible: Story = {
  args: {
    isVisible: true,
    animationDuration: 800,
  },
  parameters: {
    docs: {
      description: {
        story: "Скорборд видим на экране.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие переключателя видимости
    const toggle = canvasElement.querySelector('input[type="checkbox"]');
    expect(toggle).toBeInTheDocument();
  },
};

export const Hidden: Story = {
  args: {
    isVisible: false,
    animationDuration: 800,
  },
  parameters: {
    docs: {
      description: {
        story: "Скорборд скрыт с экрана.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем состояние переключателя
    const toggle = canvasElement.querySelector('input[type="checkbox"]');
    expect(toggle).toBeInTheDocument();
  },
};

export const FastAnimation: Story = {
  args: {
    isVisible: true,
    animationDuration: 300,
  },
  parameters: {
    docs: {
      description: {
        story: "Быстрая анимация появления/исчезновения.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие слайдера времени анимации
    const slider = canvasElement.querySelector('input[type="range"]');
    expect(slider).toBeInTheDocument();
  },
};

export const SlowAnimation: Story = {
  args: {
    isVisible: true,
    animationDuration: 1500,
  },
  parameters: {
    docs: {
      description: {
        story: "Медленная анимация появления/исчезновения.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие лейбла времени анимации
    const labels = canvasElement.querySelectorAll("label");
    expect(labels.length).toBeGreaterThan(0);
  },
};

export const NoAnimation: Story = {
  args: {
    isVisible: true,
    animationDuration: 0,
  },
  parameters: {
    docs: {
      description: {
        story: "Без анимации (мгновенное появление/исчезновение).",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие карточки
    const card = canvasElement.querySelector('[class*="card"]');
    expect(card).toBeInTheDocument();
  },
};

export const MaximumAnimation: Story = {
  args: {
    isVisible: true,
    animationDuration: 3000,
  },
  parameters: {
    docs: {
      description: {
        story: "Максимальное время анимации.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие всех элементов управления
    const controls = canvasElement.querySelectorAll('[class*="form-control"]');
    expect(controls.length).toBeGreaterThan(0);
  },
};
