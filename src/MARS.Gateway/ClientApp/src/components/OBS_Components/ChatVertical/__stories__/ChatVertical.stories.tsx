import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import ChatVertical from "../ChatVertical";
import { MessageManager } from "./MessageManager";

const meta: Meta<typeof ChatVertical> = {
  title: "Stream Components/ChatVertical",
  component: ChatVertical,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Вертикальный чат с сообщениями, которые появляются снизу и исчезают сверху.",
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
  render: () => (
    <MessageManager>
      {(messages, removeMessage) => (
        <ChatVertical messages={messages} onRemoveMessage={removeMessage} />
      )}
    </MessageManager>
  ),
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем контейнер чата
    const chatContainer = canvasElement.querySelector(
      '[class*="chatContainer"]'
    );
    expect(chatContainer).toBeInTheDocument();

    // Проверяем стили контейнера
    const containerStyle = globalThis.getComputedStyle(chatContainer!);
    expect(containerStyle.display).toBe("flex");
    expect(containerStyle.flexDirection).toBe("column-reverse");

    // Проверяем наличие кнопки генерации
    const generateButton = canvasElement.querySelector("button");
    expect(generateButton).toBeInTheDocument();
    expect(generateButton).toHaveTextContent("Generate Messages");

    // Нажимаем кнопку для генерации сообщений
    generateButton?.click();

    // Ждем появления сообщений
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Проверяем, что сообщения появились
    let messages = canvasElement.querySelectorAll('[class*="container"]');

    // Если сообщения еще не появились, ждем еще немного
    if (messages.length === 0) {
      await new Promise(resolve => setTimeout(resolve, 1000));
      messages = canvasElement.querySelectorAll('[class*="container"]');
    }

    expect(messages.length).toBeGreaterThan(0);

    // Проверяем структуру первого сообщения
    const firstMessage = messages[0];
    expect(firstMessage.className).toContain("container");

    // Проверяем наличие левой части (никнейм и бейджи)
    const leftPart = firstMessage.querySelector('[class*="left"]');
    expect(leftPart).toBeInTheDocument();

    // Проверяем наличие правой части (текст сообщения)
    const rightPart = firstMessage.querySelector('[class*="right"]');
    expect(rightPart).toBeInTheDocument();

    // Проверяем никнейм
    const nickname = firstMessage.querySelector('[class*="nickname"]');
    expect(nickname).toBeInTheDocument();
  },
};
