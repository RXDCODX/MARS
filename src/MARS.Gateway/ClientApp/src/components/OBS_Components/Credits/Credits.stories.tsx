import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import Credits from "./Credits";

const meta: Meta<typeof Credits> = {
  title: "OBS Components/Credits",
  component: Credits,
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
