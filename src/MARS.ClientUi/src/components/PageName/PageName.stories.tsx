import type { Meta, StoryObj } from "@storybook/react-vite";

import PageName from "./PageName";

const meta: Meta<typeof PageName> = {
  title: "Components/PageName",
  component: PageName,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент для отображения названия страницы.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    title: { control: "text" },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: "Главная страница",
  },
};

export const LongName: Story = {
  args: {
    title: "Очень длинное название страницы для демонстрации",
  },
};

export const ShortName: Story = {
  args: {
    title: "Чат",
  },
};

export const WithSpecialCharacters: Story = {
  args: {
    title: "Страница с символами: !@#$%^&*()",
  },
};
