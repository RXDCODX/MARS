import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import PNGTuber from "./PNGTuber";

const meta: Meta<typeof PNGTuber> = {
  title: "OBS Components/PNGTuber",
  component: PNGTuber,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  args: {
    size: 220,
    mediaStream: typeof MediaStream === "undefined" ? null : new MediaStream(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    expect(canvasElement).toBeInTheDocument();
    const image = canvasElement.querySelector("img");
    expect(image).toBeInTheDocument();
  },
};
