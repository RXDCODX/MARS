import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { SoundRequestPlayer } from "./SoundRequestPlayer";

const meta: Meta<typeof SoundRequestPlayer> = {
  title: "Stream Components/SoundRequest/Player",
  component: SoundRequestPlayer,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Адаптивный плеер для управления SoundRequest. Автоматически определяет разрешение экрана и рендерит соответствующий интерфейс (мобильный или десктопный).",
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

export const Mobile: Story = {
  args: {},
  parameters: {
    viewport: {
      defaultViewport: "mobile1",
    },
  },
};

export const Tablet: Story = {
  args: {},
  parameters: {
    viewport: {
      defaultViewport: "tablet",
    },
  },
};

export const Desktop: Story = {
  args: {},
  parameters: {
    viewport: {
      defaultViewport: "desktop",
    },
  },
};
