import type { Meta, StoryObj } from "@storybook/react-vite";
import { useState } from "react";

import { ElasticSlider } from "./ElasticSlider";

const meta: Meta<typeof ElasticSlider> = {
  title: "Stream Components/SoundRequest/Player/ElasticSlider",
  component: ElasticSlider,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Интерактивный слайдер с эластичной анимацией для управления громкостью. Основан на framer-motion для плавных анимаций.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    value: {
      control: { type: "range", min: 0, max: 100, step: 1 },
      description: "Текущее значение слайдера",
    },
    min: {
      control: { type: "number" },
      description: "Минимальное значение",
    },
    max: {
      control: { type: "number" },
      description: "Максимальное значение",
    },
    step: {
      control: { type: "number" },
      description: "Шаг изменения значения",
    },
    disabled: {
      control: { type: "boolean" },
      description: "Отключить слайдер",
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

// Обертка для использования состояния
function ElasticSliderWithState(arguments_: {
  value: number;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
}) {
  const [value, setValue] = useState(arguments_.value);

  return (
    <div style={{ width: "400px", padding: "2rem" }}>
      <ElasticSlider {...arguments_} value={value} onChange={setValue} />
    </div>
  );
}

export const Default: Story = {
  args: {
    value: 50,
    min: 0,
    max: 100,
    step: 1,
    disabled: false,
  },
  render: arguments_ => <ElasticSliderWithState {...arguments_} />,
};

export const LowVolume: Story = {
  args: {
    value: 10,
    min: 0,
    max: 100,
    step: 1,
    disabled: false,
  },
  render: arguments_ => <ElasticSliderWithState {...arguments_} />,
};

export const HighVolume: Story = {
  args: {
    value: 90,
    min: 0,
    max: 100,
    step: 1,
    disabled: false,
  },
  render: arguments_ => <ElasticSliderWithState {...arguments_} />,
};

export const Disabled: Story = {
  args: {
    value: 50,
    min: 0,
    max: 100,
    step: 1,
    disabled: true,
  },
  render: arguments_ => <ElasticSliderWithState {...arguments_} />,
};

export const CustomRange: Story = {
  args: {
    value: 25,
    min: 0,
    max: 50,
    step: 5,
    disabled: false,
  },
  render: arguments_ => <ElasticSliderWithState {...arguments_} />,
};
