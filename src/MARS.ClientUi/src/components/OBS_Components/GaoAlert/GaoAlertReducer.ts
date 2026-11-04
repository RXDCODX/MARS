import { GaoAlertDto } from "@/shared/api";

export interface GaoAlertState {
  queue: GaoAlertDto[];
  current: GaoAlertDto | null;
  isProcessing: boolean;
}

enum StateStatus {
  add,
  remove,
}

export type GaoAlertAction =
  | { type: StateStatus.add; payload: GaoAlertDto }
  | { type: StateStatus.remove; payload: GaoAlertDto };

export const initialState: GaoAlertState = {
  queue: [],
  current: null,
  isProcessing: false,
};

export function gaoAlertReducer(
  state: GaoAlertState,
  action: GaoAlertAction
): GaoAlertState {
  console.warn(state, action);

  switch (action.type) {
    case StateStatus.add: {
      if (!action.payload) {
        return state;
      }

      if (state.current?.id === action.payload.id) {
        return state;
      }

      if (state.queue.some(e => e.id === action.payload.id)) {
        return state;
      }

      if (!state.isProcessing) {
        return {
          ...state,
          queue: [...state.queue],
          current: action.payload,
          isProcessing: true,
        };
      }

      return { ...state, queue: [...state.queue, action.payload] };
    }

    case StateStatus.remove: {
      if (action.payload === undefined) {
        return { ...state };
      }

      if (state.queue.length > 0) {
        const newArray = state.queue.filter(
          item => item.id !== action.payload!.id
        );

        if (newArray.length > 0) {
          const nextAlert = newArray[0];

          return {
            ...state,
            queue: newArray,
            current: nextAlert,
            isProcessing: true,
          };
        }

        return {
          ...state,
          isProcessing: false,
          queue: newArray,
          current: null,
        };
      }

      return {
        ...state,
        queue: [],
        current: null,
        isProcessing: false,
      };
    }

    default: {
      return state;
    }
  }
}
