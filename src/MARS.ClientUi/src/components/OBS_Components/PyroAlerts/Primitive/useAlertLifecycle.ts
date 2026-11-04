import { useCallback, useEffect, useRef } from "react";

import { MediaDto } from "@/shared/api";
import { useAlertPlacementStore } from "@/shared/stores/alertPlacementStore";

interface UseAlertLifecycleOptions {
  mediaInfo: MediaDto;
  containerRef: React.RefObject<HTMLDivElement | null>;
  isEnabled: boolean;
}

export function useAlertLifecycle({
  mediaInfo,
  containerRef,
  isEnabled,
}: UseAlertLifecycleOptions) {
  const { id, metaInfo, positionInfo } = mediaInfo.mediaInfo;
  const registerAlert = useAlertPlacementStore(s => s.registerAlert);
  const unregisterAlert = useAlertPlacementStore(s => s.unregisterAlert);
  const updateAlertBounds = useAlertPlacementStore(s => s.updateAlertBounds);
  const updateRemainingTime = useAlertPlacementStore(
    s => s.updateRemainingTime
  );
  const remainingReference = useRef(metaInfo.duration);
  const clampedReference = useRef(false);

  // Clamp bounds: adjust CSS left/top so the container (media + text) stays within viewport
  const clampBounds = useCallback(() => {
    if (!isEnabled || !positionInfo.randomCoordinates) {
      return;
    }

    const container = containerRef.current;
    if (!container) {
      return;
    }

    const rect = container.getBoundingClientRect();

    // Desired bounding box position (clamped to viewport)
    let desiredLeft = rect.left;
    let desiredTop = rect.top;

    if (rect.right > window.innerWidth) {
      desiredLeft = window.innerWidth - rect.width;
    }
    if (rect.bottom > window.innerHeight) {
      desiredTop = window.innerHeight - rect.height;
    }
    if (desiredLeft < 0) {
      desiredLeft = 0;
    }
    if (desiredTop < 0) {
      desiredTop = 0;
    }

    // Delta between desired and actual bounding box position
    const deltaX = desiredLeft - rect.left;
    const deltaY = desiredTop - rect.top;

    // No adjustment needed
    if (Math.abs(deltaX) < 1 && Math.abs(deltaY) < 1) {
      updateAlertBounds(id, {
        x: rect.left,
        y: rect.top,
        width: rect.width,
        height: rect.height,
      });
      return;
    }

    // Apply delta to CSS left/top — this moves the entire rotated bounding box
    const currentLeft = Number.parseFloat(container.style.left || "0");
    const currentTop = Number.parseFloat(container.style.top || "0");
    container.style.left = `${currentLeft + deltaX}px`;
    container.style.top = `${currentTop + deltaY}px`;

    // Re-measure after adjustment
    const adjustedRect = container.getBoundingClientRect();
    updateAlertBounds(id, {
      x: adjustedRect.left,
      y: adjustedRect.top,
      width: adjustedRect.width,
      height: adjustedRect.height,
    });
  }, [
    id,
    isEnabled,
    positionInfo.randomCoordinates,
    containerRef,
    updateAlertBounds,
  ]);

  // Register / unregister
  useEffect(() => {
    if (!isEnabled || !positionInfo.randomCoordinates) {
      return;
    }

    const container = containerRef.current;
    if (!container) {
      return;
    }

    const rect = container.getBoundingClientRect();
    registerAlert({
      id,
      x: rect.left,
      y: rect.top,
      width: rect.width,
      height: rect.height,
      remainingTime: metaInfo.duration,
      totalDuration: metaInfo.duration,
      registeredAt: Date.now(),
    });

    return () => {
      unregisterAlert(id);
    };
  }, [
    id,
    isEnabled,
    positionInfo.randomCoordinates,
    metaInfo.duration,
    registerAlert,
    unregisterAlert,
    containerRef,
  ]);

  // Update remaining time every second
  useEffect(() => {
    if (!isEnabled || !positionInfo.randomCoordinates) {
      return;
    }

    remainingReference.current = metaInfo.duration;

    const timer = setInterval(() => {
      remainingReference.current = Math.max(0, remainingReference.current - 1);
      updateRemainingTime(id, remainingReference.current);
    }, 1000);

    return () => clearInterval(timer);
  }, [
    id,
    isEnabled,
    positionInfo.randomCoordinates,
    metaInfo.duration,
    updateRemainingTime,
  ]);

  // ResizeObserver: continuously clamp bounds when container size changes
  // This catches Textfit measuring text after initial render
  useEffect(() => {
    if (!isEnabled || !positionInfo.randomCoordinates) {
      return;
    }

    const container = containerRef.current;
    if (!container) {
      return;
    }

    clampedReference.current = false;

    const observer = new ResizeObserver(() => {
      // Debounce: only clamp once per frame
      if (clampedReference.current) {
        return;
      }

      clampedReference.current = true;
      requestAnimationFrame(() => {
        clampBounds();
        clampedReference.current = false;
      });
    });

    observer.observe(container);

    // Initial clamp after a short delay (lets first render settle)
    const timer = setTimeout(() => clampBounds(), 100);

    return () => {
      observer.disconnect();
      clearTimeout(timer);
    };
  }, [isEnabled, positionInfo.randomCoordinates, containerRef, clampBounds]);

  return { clampBounds };
}
