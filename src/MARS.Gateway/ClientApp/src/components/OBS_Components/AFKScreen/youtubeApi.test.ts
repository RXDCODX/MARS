import { afterEach, describe, expect, it } from "vitest";

import { ensureYouTubeApiAsync, isYouTubeApiReady } from "./youtubeApi";

/**
 * Готовность IFrame API YouTube.
 *
 * Проверка в самом компоненте спрашивала `if (!globalThis.YT)` и считала API
 * готовым по одному наличию объекта. Экран `/afkscreen` на CI падал: библиотека
 * создаёт `window.YT` раньше, чем появляется конструктор `Player`, проверка
 * проходила, и `new globalThis.YT.Player(...)` бросал «is not a constructor».
 * Исключение из эффекта ловила граница ошибок — вместо экрана зритель видел
 * заглушку. Локально состояние не успевало возникнуть, и дефект был виден только
 * в CI.
 *
 * То есть проверять надо не объект, а конструктор: `YT` без `Player` — это ещё
 * не готовое API.
 */

/** Объявляет API готовым так, как это делает библиотека. */
const declareApiReady = () => {
  globalThis.YT = {
    Player: function PlayerStub() {
      return undefined;
    },
    PlayerState: { ENDED: 0 },
  } as unknown as typeof globalThis.YT;
};

const iframeApiScripts = () =>
  Array.from(
    document.querySelectorAll<HTMLScriptElement>(
      'script[src="https://www.youtube.com/iframe_api"]'
    )
  );

afterEach(() => {
  delete (globalThis as { YT?: unknown }).YT;
  delete (globalThis as { onYouTubeIframeAPIReady?: unknown })
    .onYouTubeIframeAPIReady;

  for (const script of iframeApiScripts()) {
    script.remove();
  }
});

describe("готовность IFrame API YouTube", () => {
  it("объект YT без конструктора Player не считается готовым", () => {
    // Ровно то состояние, из-за которого экран падал в CI: объект есть,
    // конструктора в нём ещё нет.
    globalThis.YT = {} as unknown as typeof globalThis.YT;

    expect(isYouTubeApiReady()).toBe(false);
  });

  it("готовый API не вставляет скрипт и не ждёт", async () => {
    declareApiReady();

    await expect(ensureYouTubeApiAsync()).resolves.toBe("ready");
    expect(iframeApiScripts()).toHaveLength(0);
  });

  it("готовность наступает, когда библиотека объявила Player и позвала обработчик", async () => {
    const pending = ensureYouTubeApiAsync();

    // Библиотека пришла и сообщила о готовности — как это делает iframe_api.
    declareApiReady();
    globalThis.onYouTubeIframeAPIReady?.();

    await expect(pending).resolves.toBe("ready");
    expect(iframeApiScripts()).toHaveLength(1);
  });

  it("API, которое не приходит, даёт unavailable, а не исключение", async () => {
    // Скрипт в jsdom не грузится, обработчик никто не зовёт. Компонент обязан
    // получить отказ и показать свой экран ошибки, а не упасть в границу ошибок.
    await expect(ensureYouTubeApiAsync(50)).resolves.toBe("unavailable");
    expect(isYouTubeApiReady()).toBe(false);
  });

  it("повторный вызов не вставляет второй скрипт", async () => {
    await ensureYouTubeApiAsync(50);
    await ensureYouTubeApiAsync(50);

    expect(iframeApiScripts()).toHaveLength(1);
  });
});
