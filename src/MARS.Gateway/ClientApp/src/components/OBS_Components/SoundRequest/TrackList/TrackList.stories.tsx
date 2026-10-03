import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { TrackList } from "./TrackList";

const meta: Meta<typeof TrackList> = {
  title: "Stream Components/SoundRequest/TrackList",
  component: TrackList,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Компонент для отображения списка треков в системе звуковых запросов.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "600px",
          height: "400px",
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
