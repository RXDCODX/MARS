import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import type { GaoAlertDto } from "@/shared/api";

import { GaoAlert } from "./GaoAlert";

const mockDto: GaoAlertDto = {
  id: "00000000-0000-0000-0000-000000000001",
  isJustText: true,
  justText: "GAO alert preview",
};

const meta: Meta<typeof GaoAlert> = {
  title: "OBS Components/GaoAlert",
  component: GaoAlert,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
  args: {
    dto: mockDto,
    onComplete: () => {},
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    expect(canvasElement).toBeInTheDocument();
    expect(canvasElement.textContent).toContain("GAO alert preview");
  },
};
