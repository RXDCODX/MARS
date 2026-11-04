import type { Meta, StoryObj } from "@storybook/react-vite";

import AFKScreen from "./AFKScreen";

const meta: Meta<typeof AFKScreen> = {
  title: "OBS Components/AFKScreen",
  component: AFKScreen,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Компонент AFK экрана с использованием YouTube IFrame API. " +
          "Автоматически воспроизводит YouTube видео из плейлиста, " +
          "включает автозапуск, зацикливание и управление звуком. " +
          "Использует официальную библиотеку youtube-player для надежной работы.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    videoId: {
      control: "text",
      description: "ID YouTube видео",
      defaultValue: "6oMsWcCDGnw",
    },
    playlistId: {
      control: "text",
      description: "ID YouTube плейлиста",
      defaultValue: "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
    },
    autoplay: {
      control: "boolean",
      description: "Автоматический запуск воспроизведения",
      defaultValue: true,
    },
    controls: {
      control: "boolean",
      description: "Показ элементов управления",
      defaultValue: true,
    },
    loop: {
      control: "boolean",
      description: "Зацикливание видео",
      defaultValue: true,
    },
    muted: {
      control: "boolean",
      description: "Запуск без звука",
      defaultValue: true,
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
  parameters: {
    docs: {
      description: {
        story:
          "Основной вид компонента с автоматическим воспроизведением YouTube видео. " +
          "Плеер автоматически запускается, зацикливается и воспроизводит видео из плейлиста. " +
          "Включает обработку событий готовности, изменения состояния и ошибок.",
      },
    },
  },
};

export const CustomVideo: Story = {
  args: {
    videoId: "dQw4w9WgXcQ",
    playlistId: "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
  },
  parameters: {
    docs: {
      description: {
        story:
          "Компонент с кастомным видео и плейлистом. " +
          "Демонстрирует использование пропсов для настройки воспроизведения.",
      },
    },
  },
};

export const NoControls: Story = {
  args: {
    controls: false,
    muted: false,
  },
  parameters: {
    docs: {
      description: {
        story:
          "Компонент без элементов управления и со звуком. " +
          "Подходит для фонового воспроизведения в OBS.",
      },
    },
  },
};

export const Fullscreen: Story = {
  args: {},
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        story:
          "Компонент в полноэкранном режиме для OBS. " +
          "Плеер занимает весь экран и автоматически адаптируется под размер контейнера.",
      },
    },
  },
};
