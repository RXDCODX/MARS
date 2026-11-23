import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { SoundRequestPlayerDesktop } from "./SoundRequestPlayerDesktop";

const meta: Meta<typeof SoundRequestPlayerDesktop> = {
  title: "Stream Components/SoundRequest/Player/Desktop",
  component: SoundRequestPlayerDesktop,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Десктопная версия плеера для управления SoundRequest. Включает управление воспроизведением, громкостью и отображение очереди треков.",
      },
    },
  },
  tags: ["autodocs"],
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
  },
};
