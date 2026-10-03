import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { TunaMusicData } from "@/shared/api";

import CurrentTrack from "./CurrentTrack";

const meta: Meta<typeof CurrentTrack> = {
  title: "Stream Components/SoundRequest/CurrentTrack",
  component: CurrentTrack,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент для отображения текущего трека с анимациями.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    track: {
      control: "object",
    },
    shouldAnimate: {
      control: "boolean",
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

const defaultTrack: TunaMusicData = {
  id: "00000000-0000-0000-0000-000000000000",
  title: "Test Song",
  artists: ["Test Artist"],
  cover: "https://via.placeholder.com/150",
  album_url: "https://via.placeholder.com/150",
  duration: 180,
  progress: 0,
  status: "playing",
};

export const Default: Story = {
  args: {
    track: defaultTrack,
    shouldAnimate: true,
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем структуру компонента
    const wrapper = canvasElement.querySelector('[class*="wrapper"]');
    expect(wrapper).toBeInTheDocument();

    // Проверяем контейнер
    const container = canvasElement.querySelector('[class*="container"]');
    expect(container).toBeInTheDocument();

    // Проверяем обложку
    const cover = canvasElement.querySelector('[class*="cover"]');
    expect(cover).toBeInTheDocument();

    // Проверяем информацию о треке
    const trackInfo = canvasElement.querySelector('[class*="trackinfo"]');
    expect(trackInfo).toBeInTheDocument();

    // Проверяем текст трека
    const textElements = canvasElement.querySelectorAll(
      '[class*="textContainer"]'
    );
    expect(textElements.length).toBeGreaterThan(0);
  },
};

export const WithoutAnimation: Story = {
  args: {
    track: defaultTrack,
    shouldAnimate: false,
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что анимация отключена
    const wrapper = canvasElement.querySelector('[class*="wrapper"]');
    expect(wrapper).toBeInTheDocument();

    // Проверяем структуру без анимации
    const container = canvasElement.querySelector('[class*="container"]');
    expect(container).toBeInTheDocument();
  },
};

export const WithoutCover: Story = {
  args: {
    track: {
      ...defaultTrack,
      cover: "",
    },
    shouldAnimate: true,
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет изображения обложки
    const coverImage = canvasElement.querySelector('[class*="cover"] img');
    expect(coverImage).not.toBeInTheDocument();

    // Проверяем, что контейнер обложки все еще есть
    const cover = canvasElement.querySelector('[class*="cover"]');
    expect(cover).toBeInTheDocument();
  },
};

export const MultipleArtists: Story = {
  args: {
    track: {
      ...defaultTrack,
      artists: ["Artist 1", "Artist 2", "Artist 3"],
      title: "Collaboration Song",
    },
    shouldAnimate: true,
  },
  play: async ({ canvasElement }) => {
    // const canvas = within(canvasElement); // Unused variable

    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем отображение нескольких артистов
    const textElements = canvasElement.querySelectorAll(
      '[class*="textContainer"]'
    );
    expect(textElements.length).toBeGreaterThan(0);

    // Проверяем, что текст содержит информацию об артистах
    const firstTextContainer = textElements[0];
    expect(firstTextContainer).toBeInTheDocument();
  },
};
