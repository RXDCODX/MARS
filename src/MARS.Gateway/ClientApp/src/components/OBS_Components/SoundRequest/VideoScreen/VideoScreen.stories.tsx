import { useEffect } from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { FakeHubAdapter } from "@/shared/realtime/FakeHubAdapter";
import { setOverlayAdapter } from "@/shared/realtime/overlayHub";

import { VideoScreen } from "./VideoScreen";

/**
 * Стор с подключённой подделкой адаптера.
 *
 * Раньше история оборачивалась в `SoundRequestHubSignalRHubWrapper` из
 * react-signalr, и обёртка пыталась открыть настоящее соединение с хабом
 * SoundRequest. Хаба такого на сервере нет, так что история работала только
 * потому, что Storybook запускают без бэкенда и ошибка уходила в консоль.
 *
 * Теперь соединения нет вовсе: подделка отвечает на подписку и не ходит в
 * сеть. История перестаёт зависеть от того, есть ли на стенде хаб.
 */
function WithHubAdapter({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    const adapter = new FakeHubAdapter();

    setOverlayAdapter(adapter);

    return () => setOverlayAdapter(null);
  }, []);

  return <>{children}</>;
}

const meta: Meta<typeof VideoScreen> = {
  title: "Stream Components/SoundRequest/VideoScreen",
  component: VideoScreen,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Компонент для отображения видео экрана в системе звуковых запросов. Подписывается на хаб оверлея и отображает текущий воспроизводимый трек с видео.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <WithHubAdapter>
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
      </WithHubAdapter>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithNoVideo: Story = {
  play: async ({ canvasElement }) => {
    await expect(canvasElement).toBeTruthy();
  },
};
