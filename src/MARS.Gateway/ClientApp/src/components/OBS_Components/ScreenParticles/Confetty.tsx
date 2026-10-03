import { useEffect } from "react";
import Fireworks from "react-canvas-confetti/dist/presets/fireworks";
import SchoolPride from "react-canvas-confetti/dist/presets/pride";

import { randomInRange } from ".";
const defaultOptions: confetti.Options = {
  spread: randomInRange(50, 70),
  particleCount: randomInRange(50, 100),
};

const variants: confetti.Options[] = [
  // Top
  {
    ...defaultOptions,
    angle: 315,
    origin: { y: 0, x: 0 },
  },
  {
    ...defaultOptions,
    angle: 270,
    origin: { y: 0, x: 0.5 },
  },
  {
    ...defaultOptions,
    angle: 235,
    origin: { y: 0, x: 0.5 },
  },
  // Middle
  {
    ...defaultOptions,
    angle: 0,
    origin: { y: 0.5, x: 0 },
  },
  {
    ...defaultOptions,
    startVelocity: 30,
    spread: 360,
    zIndex: 0,
    origin: { y: 0.5, x: 0.5 },
  },
  {
    ...defaultOptions,
    angle: 180,
    origin: { y: 0.5, x: 1 },
  },
  // Bottom
  {
    ...defaultOptions,
    angle: 45,
    origin: { y: 1, x: 0 },
  },
  {
    ...defaultOptions,
    angle: 90,
    origin: { y: 1, x: 0.5 },
  },
  {
    ...defaultOptions,
    angle: 135,
    origin: { y: 1, x: 1 },
  },
];

interface Properties {
  callback: () => void;
}

export function Confettyv1({}: Properties) {
  const randomVariant = variants[Math.floor(Math.random() * variants.length)];

  return (
    <Fireworks
      width="100%"
      height="100%"
      autorun={{ speed: 3, duration: 5000, delay: 5 }}
      decorateOptions={() => randomVariant}
    ></Fireworks>
  );
}

export function Confettyv2({ callback }: Properties) {
  const duration = 10_000;

  useEffect(() => {
    setTimeout(() => callback(), duration);
  }, [callback]);

  return (
    <>
      <SchoolPride
        width="100%"
        height="100%"
        autorun={{ speed: 30, duration }}
        decorateOptions={(): confetti.Options => ({
          particleCount: 2,
          angle: 60,
          spread: 55,
          origin: { x: 0 },
          colors: ["#000000", "#FF0000", "#FFFFFF"],
        })}
      />
      <SchoolPride
        width="100%"
        height="100%"
        autorun={{ speed: 30, duration }}
        decorateOptions={(): confetti.Options => ({
          particleCount: 2,
          angle: 120,
          spread: 55,
          origin: { x: 1 },
          colors: ["#000000", "#FF0000", "#FFFFFF"],
        })}
      />
    </>
  );
}
