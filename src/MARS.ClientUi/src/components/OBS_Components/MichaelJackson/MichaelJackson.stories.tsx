import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import MichaelJackson from "./MichaelJackson";

const meta: Meta<typeof MichaelJackson> = {
  title: "OBS Components/MichaelJackson",
  component: MichaelJackson,
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
