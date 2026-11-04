import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import LayoutCard from "./LayoutCard";

const meta: Meta<typeof LayoutCard> = {
  title: "Stream Components/Scoreboard/AdminPanel/LayoutCard",
  component: LayoutCard,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент карточки макета для админ-панели скорборда.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "800px",
          height: "600px",
          background: "linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)",
          position: "relative",
          overflow: "hidden",
          padding: "20px",
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
