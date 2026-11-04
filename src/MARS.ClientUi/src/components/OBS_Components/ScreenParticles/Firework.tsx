import { useEffect } from "react";
import Fireworks from "react-canvas-confetti/dist/presets/fireworks";

interface Properties {
  callback: () => void;
}

export default function Firework({ callback }: Properties) {
  const duration = 10_000;

  useEffect(() => {
    setTimeout(() => callback(), duration);
  }, [callback]);

  return (
    <Fireworks width="100%" height="100%" autorun={{ speed: 3, duration }} />
  );
}
