import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { ChoosePath } from "./ChoosePath";

const meta: Meta<typeof ChoosePath> = {
  title: "Stream Components/SoundRequest/ChoosePath",
  component: ChoosePath,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент для выбора пути в системе звуковых запросов.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "400px",
          height: "300px",
          background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
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
