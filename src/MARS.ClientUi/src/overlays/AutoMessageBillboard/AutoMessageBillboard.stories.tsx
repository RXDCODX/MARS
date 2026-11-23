import type { Meta, StoryObj } from "@storybook/react-vite";

import AutoMessageBillboard from "./AutoMessageBillboard";

const meta: Meta<typeof AutoMessageBillboard> = {
  title: "Stream Components/AutoMessageBillboard",
  component: AutoMessageBillboard,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
  argTypes: {},
};

export default meta;
type Story = StoryObj<typeof meta>;

const mockMessages = [
  "Привет всем! Как дела?",
  "Не забудьте подписаться на канал!",
  "Спасибо за поддержку!",
  "Сегодня отличный день для стрима!",
  "Донатеры - вы лучшие!",
];

export const Default: Story = {
  args: {
    messages: mockMessages,
  },
  parameters: {
    docs: {
      description: {
        story:
          "Компонент показывает сообщения по одному с задержкой 2 секунды между ними. Сообщения вылетают справа по центру экрана.",
      },
    },
  },
};

export const WithMockMessages: Story = {
  args: {
    messages: mockMessages,
  },
  parameters: {
    docs: {
      description: {
        story:
          "Компонент для отображения автоматических сообщений в виде билборда, вылетающего справа по центру экрана. Показывает только одно сообщение за раз с очередью.",
      },
    },
  },
};

// Демонстрация с эмодзи
export const WithEmotes: Story = {
  args: {
    messages: [
      "Kappa это круто!",
      "PogChamp момент!",
      "FeelsGoodMan",
      "monkaS",
      "LUL",
      "PepeHands",
    ],
  },
  parameters: {
    docs: {
      description: {
        story:
          "Демонстрация работы компонента с поддержкой эмодзи 7TV, BTTV, FFZ и Twitch. Эмодзи отображаются как изображения.",
      },
    },
  },
};

// Демонстрация с длинными сообщениями
export const LongMessages: Story = {
  args: {
    messages: [
      "Это очень длинное сообщение, которое может содержать много текста и проверяет, как компонент обрабатывает перенос строк",
      "Короткое сообщение",
      "Еще одно длинное сообщение с множеством слов и деталей, чтобы проверить адаптивность компонента",
      "Финальное сообщение",
    ],
  },
  parameters: {
    docs: {
      description: {
        story:
          "Демонстрация работы с длинными сообщениями. Компонент автоматически переносит текст и адаптируется под размер.",
      },
    },
  },
};

// Демонстрация без внешних сообщений (для реального использования)
export const SignalRMode: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Режим для реального использования через SignalR. Сообщения будут приходить с сервера.",
      },
    },
  },
};
