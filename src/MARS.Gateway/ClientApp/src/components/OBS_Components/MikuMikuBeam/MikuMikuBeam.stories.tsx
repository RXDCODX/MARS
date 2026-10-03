import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import MikuMikuBeamComponent from "./MikuMikuBeamComponent";

const meta: Meta<typeof MikuMikuBeamComponent> = {
  title: "OBS Components/MikuMikuBeam",
  component: MikuMikuBeamComponent,
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
