import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import TikTokLayoutManager from "./TikTokLayoutManager";

const meta: Meta<typeof TikTokLayoutManager> = {
  title: "OBS Components/TikTokBigEdit",
  component: TikTokLayoutManager,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    expect(canvasElement).toBeInTheDocument();
  },
};
