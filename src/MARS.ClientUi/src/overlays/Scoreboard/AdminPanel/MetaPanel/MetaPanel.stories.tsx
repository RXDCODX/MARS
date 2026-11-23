import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import MetaPanel from "./MetaPanel";

const meta: Meta<typeof MetaPanel> = {
  title: "Stream Components/Scoreboard/AdminPanel/MetaPanel",
  component: MetaPanel,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component:
          "Панель для настройки мета-информации скорборда: название турнира и правила боя.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    setMeta: { action: "meta changed" },
  },
  decorators: [
    Story => (
      <div
        style={{
          width: "400px",
          padding: "20px",
          background: "#f8f9fa",
        }}
      >
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    meta: {
      title: "Tournament Name",
      fightRule: "Grand Finals",
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие панели мета-информации
    const panel = canvasElement.querySelector('[class*="meta-panel"]');
    expect(panel).toBeInTheDocument();
  },
};

export const Tournament: Story = {
  args: {
    meta: {
      title: "Street Fighter 6 Championship",
      fightRule: "Grand Finals",
    },
  },
  parameters: {
    docs: {
      description: {
        story: "Мета-панель для турнирного матча.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие поля названия турнира
    const titleInput = canvasElement.querySelector(
      'input[placeholder*="title"]'
    );
    expect(titleInput).toBeInTheDocument();
  },
};

export const Exhibition: Story = {
  args: {
    meta: {
      title: "Exhibition Match",
      fightRule: "Best of 3",
    },
  },
  parameters: {
    docs: {
      description: {
        story: "Мета-панель для выставочного матча.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие поля правил боя
    const fightRuleInput = canvasElement.querySelector(
      'input[placeholder*="fight"]'
    );
    expect(fightRuleInput).toBeInTheDocument();
  },
};

export const Finals: Story = {
  args: {
    meta: {
      title: "Season Finals",
      fightRule: "Best of 5",
    },
  },
  parameters: {
    docs: {
      description: {
        story: "Мета-панель для финального матча сезона.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие всех полей ввода
    const inputs = canvasElement.querySelectorAll('input[type="text"]');
    expect(inputs.length).toBeGreaterThan(0);
  },
};

export const Empty: Story = {
  args: {
    meta: {
      title: "",
      fightRule: "",
    },
  },
  parameters: {
    docs: {
      description: {
        story: "Мета-панель с пустыми полями.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие карточки
    const card = canvasElement.querySelector('[class*="card"]');
    expect(card).toBeInTheDocument();
  },
};

export const LongTitle: Story = {
  args: {
    meta: {
      title: "Very Long Tournament Name That Might Overflow",
      fightRule: "Special Rules for This Tournament",
    },
  },
  parameters: {
    docs: {
      description: {
        story: "Мета-панель с длинным названием турнира.",
      },
    },
  },
  play: async ({ canvasElement }) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие заголовка карточки
    const cardHeader = canvasElement.querySelector('[class*="card-header"]');
    expect(cardHeader).toBeInTheDocument();
  },
};
