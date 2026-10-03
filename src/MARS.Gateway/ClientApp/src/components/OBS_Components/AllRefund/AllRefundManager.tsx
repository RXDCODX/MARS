import { useEffect, useReducer, useState } from "react";

import { TelegramusHubSignalRContext, type TwitchUser } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import AllRefund from "./AllRefund";

type Action =
  | { type: "ENQUEUE"; payload: AllRefundRequest }
  | { type: "DEQUEUE" }
  | { type: "START_PLAY" };

interface AllRefundRequest {
  id: string;
  user: TwitchUser;
}

interface State {
  queue: AllRefundRequest[];
  playing: boolean;
  current: AllRefundRequest | null;
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
        playing: rest.length > 0,
      };
    }
    case "START_PLAY": {
      if (state.playing) {
        return state;
      }

      const next = state.current ?? state.queue[0] ?? null;

      return {
        ...state,
        playing: next !== null,
        current: next,
      };
    }
    default: {
      return state;
    }
  }
}

export default function AllRefundManager() {
  const [state, dispatch] = useReducer(reducer, initialState);
  const [isAnnounced, setIsAnnounced] = useState(false);

  TelegramusHubSignalRContext.useSignalREffect(
    "AllRefund",
    (user: TwitchUser) => {
      dispatch({
        type: "ENQUEUE",
        payload: {
          id: `${user.twitchId}-${Date.now()}-${Math.random()}`,
          user,
        },
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
        <Announce title="All Refund" callback={() => setIsAnnounced(true)} />
      )}
      <div
        style={{ width: "100%", height: "100%" }}
        data-testid="obs-all-refund"
      >
        {state.playing && state.current && (
          <AllRefund
            key={state.current.id}
            user={state.current.user}
            onComplete={() => dispatch({ type: "DEQUEUE" })}
          />
        )}
      </div>
    </>
  );
}
