import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import PhonkLayoutManager from "./PhonkLayoutManager";

const meta: Meta<typeof PhonkLayoutManager> = {
  title: "OBS Components/PhonkLayout",
  component: PhonkLayoutManager,
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
