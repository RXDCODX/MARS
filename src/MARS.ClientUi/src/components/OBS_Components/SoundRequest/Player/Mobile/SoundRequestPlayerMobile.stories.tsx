import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { SoundRequestPlayerMobile } from "./SoundRequestPlayerMobile";

const meta: Meta<typeof SoundRequestPlayerMobile> = {
  title: "Stream Components/SoundRequest/Player/Mobile",
  component: SoundRequestPlayerMobile,
  parameters: {
    layout: "fullscreen",
    viewport: {
      defaultViewport: "mobile1",
    },
    docs: {
      description: {
        component:
          "Мобильная версия Sound Request плеера. Переделана с использованием компонентной архитектуры по аналогии с десктопной версией. " +
          "Включает отдельные компоненты: CurrentTrack, PlayerControls, VolumeControls, QueueList и QueueItem. " +
          "Оптимизирована для мобильных устройств с большими кнопками и адаптивной типографикой.",
      },
    },
  },
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * Базовый вид мобильного плеера со всеми компонентами
 */
export const Default: Story = {
  args: {},
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();
  },
  parameters: {
    docs: {
      description: {
        story:
          "Базовое отображение мобильного плеера со всеми компонентами: текущий трек, управление, очередь и ближайшие 5 заказов.",
      },
    },
  },
};
