import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { ADHDPage } from "./ADHDPage";

const meta: Meta<typeof ADHDPage> = {
  title: "OBS Components/ADHDLayout",
  component: ADHDPage,
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
