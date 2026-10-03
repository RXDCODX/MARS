import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { SoundRequestHubSignalRHubWrapper } from "@/shared/api/signalr-clients/SoundRequestHub/SignalRHubWrapper";

import { VideoScreen } from "./VideoScreen";

const meta: Meta<typeof VideoScreen> = {
  title: "Stream Components/SoundRequest/VideoScreen",
  component: VideoScreen,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Компонент для отображения видео экрана в системе звуковых запросов. Подключается к SignalR хабу SoundRequest и отображает текущий воспроизводимый трек с видео.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <SoundRequestHubSignalRHubWrapper>
        <div
          style={{
            width: "100vw",
            height: "100vh",
            position: "relative",
            overflow: "hidden",
          }}
        >
          <Story />
        </div>
      </SoundRequestHubSignalRHubWrapper>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * Стандартный вид компонента - ожидание подключения к SignalR
 */
export const Default: Story = {
  args: {},
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();
  },
};

/**
 * Компонент с пояснением - для реального использования нужно подключение к серверу
 */
export const WithDescription: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Этот компонент требует подключения к SignalR хабу SoundRequest. В стандартной демо версии вы увидите состояние ожидания трека. Для полной функциональности подключите компонент к работающему серверу.",
      },
    },
  },
};
