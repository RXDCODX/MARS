import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import { ErrorBoundary } from "@/shared/components/ErrorBoundary/ErrorBoundary";

import AFKScreen from "./AFKScreen.tsx";

/**
 * Экран afkscreen и IFrame API YouTube.
 *
 * Библиотека забирает переданный ей узел: создаёт рядом свой `<iframe>` и
 * заменяет им элемент. React про такой обмен не знает — в его модели узел,
 * отданный наружу, всё ещё его собственный. На следующем рендере React вставлял
 * новый узел, опираясь на украденный, и `insertBefore` бросал NotFoundError:
 * «node before which the new node is to be inserted is not a child of this
 * node». Ошибка приходила из коммита, то есть ловилась границей ошибок, и зритель
 * видел заглушку вместо экрана. Локально библиотека не успевала перехватить узел,
 * дефект проявлялся только в CI — как «упал рендер» без единого слова о причине.
 */

interface CapturedEvents {
  onError?: (event: { target: unknown; data: number }) => void;
}

afterEach(() => {
  delete (globalThis as { YT?: unknown }).YT;
});

describe("экран afkscreen", () => {
  it("переживает замену узла, которую делает библиотека", async () => {
    let captured: CapturedEvents | undefined = undefined;

    globalThis.YT = {
      Player: function PlayerStub(element: HTMLElement, options: unknown) {
        const { events } = options as { events?: CapturedEvents };
        captured = events;

        // Ровно то, что делает настоящая библиотека: узел, отданный ей,
        // перестаёт быть её ребёнком.
        const parent = element.parentNode;
        if (parent !== null) {
          parent.replaceChild(document.createElement("iframe"), element);
        }

        return {
          destroy: () => undefined,
          loadPlaylist: () => undefined,
          nextVideo: () => undefined,
          setShuffle: () => undefined,
          mute: () => undefined,
          unMute: () => undefined,
        };
      },
      PlayerState: { ENDED: 0 },
    } as unknown as typeof globalThis.YT;

    render(
      <ErrorBoundary>
        <AFKScreen />
      </ErrorBoundary>
    );

    // Эффект сначала дожидается готовности API, и только потом создаёт плеер.
    // Без паузы обработчика ещё нет, и проверка ничего не сообщала бы.
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0));
    });

    expect(captured).toBeDefined();

    // Библиотека сообщила об ошибке видео: компонент ставит `hasError` и
    // перерисовывается. Раньше этот рендер и ронял экран.
    await act(async () => {
      captured?.onError?.({ target: {}, data: 2 });
    });

    expect(screen.queryByTestId("error-boundary")).toBeNull();
    expect(screen.getByTestId("button-retry")).toBeTruthy();
  });
});
