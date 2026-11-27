import { useOverlayHubStore } from "@/shared/stores/overlayHubStore";

/**
 * Единственный способ вызвать метод AlertsHub из React-компонента.
 */
export function useOverlayHubInvoke() {
  return useOverlayHubStore(state => state.invoke);
}