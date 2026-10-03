import type { Meta, StoryObj } from "@storybook/react-vite";

import AdminPanel from "./AdminPanel";

const meta: Meta<typeof AdminPanel> = {
  title: "ControlRoom/AdminPanel",
  component: AdminPanel,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const WithCustomTheme: Story = {
  args: {},
  parameters: {
    backgrounds: {
      default: "dark",
    },
  },
};
