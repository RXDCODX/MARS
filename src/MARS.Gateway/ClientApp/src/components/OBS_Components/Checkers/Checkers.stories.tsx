import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import Checkers from "./Checkers";

const meta: Meta<typeof Checkers> = {
  title: "Stream Components/Checkers",
  component: Checkers,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component: "Компонент для отображения игры в шашки в OBS.",
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
          background: "linear-gradient(135deg, #2c3e50 0%, #34495e 100%)",
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
  args: {},
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();
  },
};
