import { useEffect, useReducer, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import { TelegramusHubSignalRContext } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import PhonkShitAlerts from "./PhonkShitAlerts";

type Action =
  | { type: "ENQUEUE"; payload: string }
  | { type: "DEQUEUE" }
  | { type: "START_PLAY" }
  | { type: "STOP_PLAY" };

interface State {
  queue: string[];
  playing: boolean;
  current: string | null;
}

const initialState: State = {
  queue: [],
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

export default function PhonkLayoutManager() {
  const [state, dispatch] = useReducer(reducer, initialState);
  const [isAnnounced, setIsAnnounced] = useState(false);

  TelegramusHubSignalRContext.useSignalREffect(
    "PhonkEdit",
    () => {
      dispatch({
        type: "ENQUEUE",
        payload: uuidv4(),
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
          title="TikTok Edit"
          callback={() => setIsAnnounced(true)}
          key={1}
        />
      )}
      <div
        style={{ width: "100%", height: "100%" }}
        data-testid="obs-phonk-layout"
      >
        {state.playing && state.current && (
          <PhonkShitAlerts
            key={state.current}
            callback={() => dispatch({ type: "DEQUEUE" })}
          />
        )}
      </div>
    </>
  );
}
