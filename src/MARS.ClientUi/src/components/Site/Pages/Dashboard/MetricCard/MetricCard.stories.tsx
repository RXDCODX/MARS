import type { Meta, StoryObj } from "@storybook/react";

import MetricCard from "./MetricCard";

const meta: Meta<typeof MetricCard> = {
  title: "ControlRoom/MetricCard",
  component: MetricCard,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  argTypes: {
    color: {
      control: { type: "select" },
      options: ["blue", "green", "purple", "orange", "red", "teal"],
    },
    trendDirection: {
      control: { type: "select" },
      options: ["up", "down", "stable"],
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const CPUUsage: Story = {
  args: {
    title: "Использование CPU",
    value: "67.5%",
    icon: "🖥️",
    color: "blue",
    trend: "+2.5%",
    trendDirection: "up",
  },
};

export const MemoryUsage: Story = {
  args: {
    title: "Использование памяти",
    value: "45.2%",
    icon: "💾",
    color: "green",
    trend: "-1.2%",
    trendDirection: "down",
  },
};

export const ActiveConnections: Story = {
  args: {
    title: "Активные соединения",
    value: "1,234",
    icon: "🔗",
    color: "purple",
    trend: "+15",
    trendDirection: "up",
  },
};

export const ErrorRate: Story = {
  args: {
    title: "Ошибки (%)",
    value: "0.8%",
    icon: "⚠️",
    color: "red",
    trend: "-0.5%",
    trendDirection: "down",
  },
};

export const Uptime: Story = {
  args: {
    title: "Время работы",
    value: "5д 12ч 34м",
    icon: "⏱️",
    color: "teal",
    trend: "Стабильно",
    trendDirection: "stable",
  },
};
