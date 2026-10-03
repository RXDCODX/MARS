import type { Meta, StoryObj } from "@storybook/react-vite";

import { TelegramusHubSignalRHubWrapper } from "@/shared/api";

import MikuMondayController from "./MikuMondayController";

const meta = {
  title: "OBS Components/MikuMonday",
  component: MikuMondayController,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    Story => (
      <TelegramusHubSignalRHubWrapper>
        <div
          style={{
            width: "100vw",
            height: "100vh",
            background: "#0e0e0e",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Story />
        </div>
      </TelegramusHubSignalRHubWrapper>
    ),
  ],
  tags: ["autodocs"],
} satisfies Meta<typeof MikuMondayController>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const WithMockData: Story = {
  args: {},
  play: async () => {
    // Эмуляция получения данных через SignalR
    const mockData = {
      id: "11111111-1111-1111-1111-111111111111",
      displayName: "rxdcodx",
      selectedTrack: {
        number: 8,
        artist: "Mikito P",
        title: "39 Music!",
        url: "https://youtu.be/OuLZlZ18APQ?si=9hgjtARksqY9f0zl",
        thumbnailUrl: "https://i.ytimg.com/vi/OuLZlZ18APQ/maxresdefault.jpg",
      },
      availableTracks: [
        {
          number: 1,
          artist: "wowaka",
          title: "Rolling Girl",
          url: "https://youtu.be/vnw8zURAxkU",
          thumbnailUrl: "https://i.ytimg.com/vi/vnw8zURAxkU/maxresdefault.jpg",
        },
        {
          number: 2,
          artist: "Kairiki Bear",
          title: "Bug",
          url: "https://youtu.be/FkO8ub83wss",
          thumbnailUrl: "https://i.ytimg.com/vi/FkO8ub83wss/maxresdefault.jpg",
        },
        {
          number: 3,
          artist: "Kanaria",
          title: "Yoidoreshirazu",
          url: "https://youtu.be/vB8sxY_PJ_w",
          thumbnailUrl: "https://i.ytimg.com/vi/vB8sxY_PJ_w/maxresdefault.jpg",
        },
        {
          number: 10,
          artist: "PinocchioP",
          title: "God-Ish",
          url: "https://youtu.be/EHBFKhLUVig",
          thumbnailUrl: "https://i.ytimg.com/vi/EHBFKhLUVig/maxresdefault.jpg",
        },
        {
          number: 19,
          artist: "ryo",
          title: "World Is Mine",
          url: "https://youtu.be/SZcilh-cZXE",
          thumbnailUrl: "https://i.ytimg.com/vi/SZcilh-cZXE/maxresdefault.jpg",
        },
      ],
    };

    // Отправляем mock данные через SignalR эмуляцию
    setTimeout(() => {
      const event = new CustomEvent("MikuMonday", { detail: mockData });
      globalThis.dispatchEvent(event);
    }, 1000);
  },
};
