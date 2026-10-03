import { ReactNode, useCallback, useEffect, useReducer, useState } from "react";

import { TelegramusHubSignalRContext } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import AlertTemplate from "./template/AlertTemplate";
import { getRandomVersion as getRandomAlertVersion } from "./versions";

type Action =
  | { type: "ENQUEUE"; payload: Request }
  | { type: "DEQUEUE" }
  | { type: "START_PLAY" }
  | { type: "STOP_PLAY" };

interface Request {
  guid: string;
  content: ReactNode;
}

interface State {
  queue: Request[];
  playing: boolean;
  current: Request | null;
}

const initialState: State = {
  queue: [] as Request[],
  playing: false,
  current: null,
};

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case "ENQUEUE": {
      return { ...state, queue: [...state.queue, action.payload] };
    }
    case "DEQUEUE": {
      const [, ...rest] = state.queue;
      return {
        ...state,
        queue: rest,
        current: rest.length > 0 ? rest[0] : null,
        playing: rest.length > 0 ? true : false,
      };
    }
    case "START_PLAY": {
      if (state.playing) return state;
      const next = state.current ?? state.queue[0] ?? null;
      return {
        ...state,
        playing: !!next,
        current: next,
      };
    }
    case "STOP_PLAY": {
      return { ...state, playing: false, current: null };
    }
    default: {
      return state;
    }
  }
}

export default function TikTokLayoutManager() {
  const [state, dispatch] = useReducer(reducer, initialState);
  const [isAnnounced, setIsAnnounced] = useState(false);
  const [lastVersionId, setLastVersionId] = useState<number>(0);

  const getRandomVersion = useCallback(
    (guid: string, text: string) => {
      let version = getRandomAlertVersion(text, () =>
        dispatch({ type: "DEQUEUE" })
      );

      while (version.id === lastVersionId) {
        version = getRandomAlertVersion(text, () =>
          dispatch({ type: "DEQUEUE" })
        );
      }

      setLastVersionId(version.id);

      return (
        <AlertTemplate
          firstPart={version.Variant.firstPart}
          secondPart={{ message: version.Variant.secondPart.message }}
          callback={() => dispatch({ type: "DEQUEUE" })}
          key={guid}
        />
      );
    },
    [lastVersionId]
  );

  TelegramusHubSignalRContext.useSignalREffect(
    "TiktokEdit",
    (guid: string, text: string) => {
      dispatch({
        type: "ENQUEUE",
        payload: { guid, content: getRandomVersion(guid, text) },
      });
    },
    []
  );

  useEffect(() => {
    if (!state.playing && state.queue.length > 0) {
      dispatch({ type: "START_PLAY" });
    }
  }, [state.queue, state.playing]);

  return (
    <>
      {!isAnnounced && (
        <Announce
          title="Phonk Layout"
          callback={() => setIsAnnounced(true)}
          key={1}
        />
      )}
      <div
        style={{ width: "100%", height: "100%" }}
        data-testid="obs-tiktok-layout"
      >
        {state.playing && state.current && state.current.content}
      </div>
    </>
  );
}
