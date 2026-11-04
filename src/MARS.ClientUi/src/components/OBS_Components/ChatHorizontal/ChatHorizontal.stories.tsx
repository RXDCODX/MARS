import type { Meta, StoryObj } from "@storybook/react-vite";
import { fireEvent } from "@storybook/testing-library";
import { expect } from "storybook/test";

import ChatHorizontal from "./ChatHorizontal";
import { MessageManager } from "./MessageManager";

const meta: Meta<typeof ChatHorizontal> = {
  title: "Stream Components/ChatHorizontal",
  component: ChatHorizontal,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Горизонтальный чат с анимированными сообщениями, которые проходят через экран.",
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
        <ChatHorizontal messages={messages} onRemoveMessage={removeMessage} />
      )}
    </MessageManager>
  ),
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие кнопки генерации
    const generateButton = canvasElement.querySelector("button");
    expect(generateButton).toBeInTheDocument();
    expect(generateButton).toHaveTextContent("Generate Messages");

    // Проверяем, что ChatHorizontal компонент отрендерился
    // ChatHorizontal не имеет контейнера, поэтому проверяем наличие кнопки и отсутствие сообщений изначально
    const initialMessages =
      canvasElement.querySelectorAll('[class*="message"]');
    expect(initialMessages.length).toBe(0);

    // Проверяем, что кнопка видима и кликабельна
    const buttonStyle = globalThis.getComputedStyle(generateButton!);
    expect(buttonStyle.display).not.toBe("none");
    expect(buttonStyle.visibility).not.toBe("hidden");

    // Нажимаем кнопку для генерации сообщений
    if (generateButton) {
      fireEvent.click(generateButton);
    }

    // Ждем появления сообщений (увеличиваем время ожидания)
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Проверяем, что сообщения появились
    let messages = canvasElement.querySelectorAll('[class*="message"]');

    // Если сообщения еще не появились, ждем еще немного
    if (messages.length === 0) {
      await new Promise(resolve => setTimeout(resolve, 1000));
      messages = canvasElement.querySelectorAll('[class*="message"]');
    }

    // Если все еще нет сообщений, попробуем еще раз
    if (messages.length === 0) {
      await new Promise(resolve => setTimeout(resolve, 1000));
      messages = canvasElement.querySelectorAll('[class*="message"]');
    }

    expect(messages.length).toBeGreaterThan(0);

    // Проверяем структуру первого сообщения
    const firstMessage = messages[0];
    expect(firstMessage.className).toContain("message");

    // Дополнительная проверка - ищем сообщения по разным селекторам
    const allMessages = canvasElement.querySelectorAll(
      '[class*="message"], [class*="container"]'
    );
    expect(allMessages.length).toBeGreaterThan(0);

    // Проверяем наличие никнейма
    const nickname = firstMessage.querySelector('[class*="nickname"]');
    expect(nickname).toBeInTheDocument();

    // Проверяем наличие текста сообщения
    const text = firstMessage.querySelector('[class*="text"]');
    expect(text).toBeInTheDocument();

    // Проверяем анимацию сообщений
    const computedStyle = globalThis.getComputedStyle(firstMessage);
    expect(computedStyle.position).toBe("absolute");
  },
};
