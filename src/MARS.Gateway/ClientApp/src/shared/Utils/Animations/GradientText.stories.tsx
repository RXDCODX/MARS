import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import GradientText from "./GradientText";

const meta: Meta<typeof GradientText> = {
  title: "Animations/GradientText",
  component: GradientText,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент для отображения текста с градиентной анимацией.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    text: {
      control: "text",
    },
    fontWeight: {
      control: "number",
    },
    speed: {
      control: "select",
      options: ["slow", "normal", "fast", "very-slow"],
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    text: "Gradient Text! ✨",
    fontWeight: 600,
    speed: "normal",
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие текста
    const textElement = canvasElement.querySelector('[class*="gradient"]');
    expect(textElement).toBeInTheDocument();

    // Проверяем, что текст отображается
    expect(textElement).toHaveTextContent("Gradient Text! ✨");

    // Проверяем стили
    const computedStyle = globalThis.getComputedStyle(textElement!);
    expect(computedStyle.fontWeight).toBe("600");
  },
};

export const SlowSpeed: Story = {
  args: {
    text: "Slow Gradient",
    fontWeight: 400,
    speed: "slow",
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем медленную анимацию
    const textElement = canvasElement.querySelector('[class*="gradient"]');
    expect(textElement).toBeInTheDocument();

    // Проверяем стили для медленной анимации
    const computedStyle = globalThis.getComputedStyle(textElement!);
    expect(computedStyle.fontWeight).toBe("400");
  },
};

export const FastSpeed: Story = {
  args: {
    text: "Fast Gradient",
    fontWeight: 700,
    speed: "fast",
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем быструю анимацию
    const textElement = canvasElement.querySelector('[class*="gradient"]');
    expect(textElement!);
    expect(textElement).toBeInTheDocument();

    // Проверяем стили для быстрой анимации
    const computedStyle = globalThis.getComputedStyle(textElement!);
    expect(computedStyle.fontWeight).toBe("700");
  },
};

export const LongText: Story = {
  args: {
    text: "This is a very long gradient text that demonstrates the animation effect across multiple words and characters with different speeds and weights!",
    fontWeight: 500,
    speed: "very-slow",
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем длинный текст
    const textElement = canvasElement.querySelector('[class*="gradient"]');
    expect(textElement).toBeInTheDocument();

    // Проверяем, что весь текст отображается
    expect(textElement).toHaveTextContent("This is a very long gradient text");

    // Проверяем стили для очень медленной анимации
    const computedStyle = globalThis.getComputedStyle(textElement!);
    expect(computedStyle.fontWeight).toBe("500");
  },
};

export const WithShadow: Story = {
  args: {
    text: "Text with Shadow",
    speed: "normal",
    shadow: {
      enabled: true,
      color: "rgba(0, 0, 0, 0.5)",
      blur: 6,
      offsetX: 3,
      offsetY: 3,
    },
  },
};

export const LargeText: Story = {
  args: {
    text: "Large Gradient Text",
    fontSize: "48px",
    fontWeight: "bold",
  },
};

export const CustomColors: Story = {
  args: {
    text: "Custom Colors",
    colors: ["#ff0000", "#00ff00", "#0000ff", "#ffff00", "#ff00ff"],
  },
};
