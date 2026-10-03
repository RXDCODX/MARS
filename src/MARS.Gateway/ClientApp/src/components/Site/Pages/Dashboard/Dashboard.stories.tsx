import type { Meta, StoryObj } from "@storybook/react";

import Dashboard from "./Dashboard";

const meta: Meta<typeof Dashboard> = {
  title: "ControlRoom/Dashboard",
  component: Dashboard,
  parameters: {
    layout: "padded",
  },
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const Loading: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story: "Состояние загрузки дашборда с анимацией спиннера.",
      },
    },
  },
};
