import { describe, expect, it } from "vitest";

import { FakeHubAdapter } from "@/shared/api/realtime/FakeHubAdapter";

import { useOverlayHubStore } from "./overlayHubStore";

/**
 * Форма событий гача так, как их читают компоненты.
 *
 * Тест скормит настоящее содержимое ветки `oneof` и сверит ключи, которые стор
 * кладёт в очередь, с теми, которые потом читает компонент. Так поймался ключ
 * `miku` вместо `mikuModule`: типы молча прощали это приведением, а компонент
 * падал на `currentMikuMessage.mikuModule.pageId`.
 *
 * У каждой г��чи своё поле состояния, поэтому и проверяется нужное: `reset`
 * общий, а `currentMessage` — только про вайфу.
 */
describe("форма событий гача в очередях стора", () => {
  const twitchUser = { twitchId: "42", displayName: "Стример" };

  it("MikuRoll кладёт mikuModule, а не miku", async () => {
    // MikuRollEvent: bytes miku_module_json, bytes twitch_user_json.
    const adapter = new FakeHubAdapter();
    useOverlayHubStore.getState().reset();
    await useOverlayHubStore.getState().start(adapter);

    adapter.emit("MikuRoll", {
      module: { pageId: "page-1", japaneseName: "初音ミク" },
      user: twitchUser,
      color: "#39c5bb",
      collectedCount: 1,
      totalCount: 5,
    });

    const current = useOverlayHubStore.getState().currentMikuMessage as
      | Record<string, unknown>
      | undefined;

    // Потребитель читает именно эти ключи, см. MikuAlerts/helper.ts.
    expect(current).toBeDefined();
    expect(current?.mikuModule).toEqual({
      pageId: "page-1",
      japaneseName: "初音ミク",
    });
    expect(current?.twitchUser).toEqual(twitchUser);
    expect(current?.miku).toBeUndefined();
  });

  it("FumoRoll кладёт fumo и twitchUser", async () => {
    // FumoRollEvent: bytes fumo_json, bytes twitch_user_json.
    const adapter = new FakeHubAdapter();
    useOverlayHubStore.getState().reset();
    await useOverlayHubStore.getState().start(adapter);

    adapter.emit("FumoRoll", {
      fumo: { mfcId: "mfc-1", title: "Фумо" },
      user: twitchUser,
      color: "#ff8800",
      collectedCount: 2,
      totalCount: 7,
    });

    const current = useOverlayHubStore.getState().currentFumoMessage as
      | Record<string, unknown>
      | undefined;

    expect(current?.fumo).toEqual({ mfcId: "mfc-1", title: "Фумо" });
    expect(current?.twitchUser).toEqual(twitchUser);
  });

  it("FrogRoll кладёт frog и twitchUser", async () => {
    // FrogRollEvent: bytes frog_json, bytes twitch_user_json.
    const adapter = new FakeHubAdapter();
    useOverlayHubStore.getState().reset();
    await useOverlayHubStore.getState().start(adapter);

    adapter.emit("FrogRoll", {
      frog: { id: "frog-1", title: "Жаба" },
      user: twitchUser,
      color: "#22aa22",
    });

    const current = useOverlayHubStore.getState().currentFrogMessage as
      | Record<string, unknown>
      | undefined;

    expect(current?.frog).toEqual({ id: "frog-1", title: "Жаба" });
    expect(current?.twitchUser).toEqual(twitchUser);
  });
});


