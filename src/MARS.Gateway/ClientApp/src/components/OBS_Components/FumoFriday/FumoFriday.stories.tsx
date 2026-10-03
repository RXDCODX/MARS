import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, within } from "storybook/test";

import { Cirno } from "./Cirno";
import { FumoFriday } from "./FumoFriday";
import { Reimu } from "./Reimu";

const meta: Meta<typeof FumoFriday> = {
  title: "Stream Components/FumoFriday",
  component: FumoFriday,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Компонент для празднования Fumo Friday с анимациями и видео.",
      },
    },
  },
  tags: ["autodocs"],
  decorators: [
    Story => (
      <div
        style={{
          width: "100vw",
          height: "100vh",
          background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
          position: "relative",
          overflow: "hidden",
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
  args: {},
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что кнопка тестирования присутствует
    const testButton = canvas.getByText("Test FumoFriday Alert");
    expect(testButton).toBeInTheDocument();
    expect(testButton).toBeEnabled();
  },
};

export const CirnoAlert: Story = {
  render: () => (
    <Cirno
      displayName={{
        id: "test-id",
        message: "Test User",
        color: "#ff6b6b",
      }}
      callback={() => console.log("Cirno alert finished")}
    />
  ),
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что видео присутствует
    const video = canvasElement.querySelector("video");
    expect(video).toBeInTheDocument();
  },
};

export const ReimuAlert: Story = {
  render: () => (
    <Reimu
      displayName={{
        id: "test-id",
        message: "Test User",
        color: "#4ecdc4",
      }}
      callback={() => console.log("Reimu alert finished")}
    />
  ),
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что видео присутствует
    const video = canvasElement.querySelector("video");
    expect(video).toBeInTheDocument();
  },
};
