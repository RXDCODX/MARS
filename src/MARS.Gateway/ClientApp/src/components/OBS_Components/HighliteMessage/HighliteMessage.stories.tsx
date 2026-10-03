import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import HighliteMessage from "./HighliteMessage";
import MessageDemo from "./MessageDemo";

const meta: Meta<typeof HighliteMessage> = {
  title: "Stream Components/HighliteMessage",
  component: HighliteMessage,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Система всплывающих сообщений с изображениями и анимациями, использующая лица из папки ассетов.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [Story => <Story />],
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

    // Проверяем, что нет активных всплывающих сообщений изначально
    const highliteMessages = canvasElement.querySelectorAll(
      '[class*="container"]'
    );
    expect(highliteMessages.length).toBe(0);

    // Проверяем, что нет изображений изначально
    const images = canvasElement.querySelectorAll("img");
    expect(images.length).toBe(0);
  },
};

export const Empty: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Пустая система всплывающих сообщений без активных событий.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет всплывающих сообщений
    const highliteMessages = canvasElement.querySelectorAll(
      '[class*="container"]'
    );
    expect(highliteMessages.length).toBe(0);

    // Проверяем, что нет изображений
    const images = canvasElement.querySelectorAll("img");
    expect(images.length).toBe(0);

    // Проверяем, что нет видео
    const videos = canvasElement.querySelectorAll("video");
    expect(videos.length).toBe(0);

    // Проверяем, что нет пузырьков с текстом
    const bubbles = canvasElement.querySelectorAll('[class*="bubble"]');
    expect(bubbles.length).toBe(0);
  },
};

// Новая история с демо-компонентом
export const Demo: Story = {
  render: () => <MessageDemo />,
  parameters: {
    docs: {
      description: {
        story:
          "Демонстрация работы компонента с тестовыми сообщениями и лицами из ассетов.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Ждем появления первого сообщения
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Проверяем, что появилось сообщение
    const messages = canvasElement.querySelectorAll('[class*="container"]');
    expect(messages.length).toBeGreaterThan(0);

    // Проверяем, что есть изображение или видео
    const mediaElements = canvasElement.querySelectorAll("img, video");
    expect(mediaElements.length).toBeGreaterThan(0);

    // Проверяем, что есть пузырьки с текстом
    const bubbles = canvasElement.querySelectorAll('[class*="bubble"]');
    expect(bubbles.length).toBeGreaterThan(0);

    // Проверяем кнопку добавления сообщений
    const addButton = canvasElement.querySelector("button");
    expect(addButton).toBeInTheDocument();
    expect(addButton).toHaveTextContent("Добавить сообщение");
  },
};

// История для тестирования только изображений
export const ImagesOnly: Story = {
  render: () => <MessageDemo />,
  parameters: {
    docs: {
      description: {
        story: "Демонстрация работы только с изображениями (GIF).",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Ждем появления сообщений
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Проверяем, что есть изображения
    const images = canvasElement.querySelectorAll("img");
    expect(images.length).toBeGreaterThan(0);

    // Проверяем, что изображения загружены
    for (const img of images) {
      expect(img).toHaveAttribute("src");
      expect(img).toHaveAttribute("alt");
    }
  },
};

// История для тестирования только видео
export const VideosOnly: Story = {
  render: () => <MessageDemo />,
  parameters: {
    docs: {
      description: {
        story: "Демонстрация работы только с видео файлами.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Ждем появления сообщений
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Проверяем, что есть видео
    const videos = canvasElement.querySelectorAll("video");
    expect(videos.length).toBeGreaterThan(0);

    // Проверяем, что видео настроены правильно
    for (const video of videos) {
      expect(video).toHaveAttribute("src");
      expect(video).toHaveAttribute("autoPlay");
      expect(video).toHaveAttribute("loop");
      expect(video).toHaveAttribute("muted");
    }
  },
};
