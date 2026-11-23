import { useEffect, useRef } from "react";

import styles from "./SilkLegacy.module.scss";

type SilkLegacyProperties = {
  speed?: number;
  scale?: number;
  color?: string;
  noiseIntensity?: number;
  rotation?: number;
};

const SilkLegacy: React.FC<SilkLegacyProperties> = ({
  speed = 5,
  scale = 1,
  color = "#7B7481",
  noiseIntensity = 1.5,
  rotation = 0,
}) => {
  const canvasReference = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;

    const context = canvas.getContext("2d", { alpha: true });
    if (!context) return;

    const parent = canvas.parentElement;
    if (!parent) return;

    let rafId = 0;
    let width = 1;
    let height = 1;

    const parseHex = (hex: string) => {
      const clean = hex.replace("#", "");
      const expanded =
        clean.length === 3
          ? clean
              .split("")
              .map(ch => `${ch}${ch}`)
              .join("")
          : clean;
      const r = Number.parseInt(expanded.slice(0, 2), 16) || 123;
      const g = Number.parseInt(expanded.slice(2, 4), 16) || 116;
      const b = Number.parseInt(expanded.slice(4, 6), 16) || 129;
      return { r, g, b };
    };

    const base = parseHex(color);

    const resize = () => {
      width = Math.max(1, Math.floor(parent.clientWidth));
      height = Math.max(1, Math.floor(parent.clientHeight));
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(width * dpr);
      canvas.height = Math.floor(height * dpr);
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const draw = (timeMs: number) => {
      const t = (timeMs / 1000) * Math.max(0.1, speed) * 0.4;
      const density = Math.max(10, Math.floor(28 * Math.max(0.2, scale)));
      const alpha = Math.min(0.45, 0.12 + noiseIntensity * 0.05);

      context.clearRect(0, 0, width, height);
      context.save();
      context.translate(width / 2, height / 2);
      context.rotate(rotation);
      context.translate(-width / 2, -height / 2);

      const gradient = context.createLinearGradient(0, 0, width, height);
      gradient.addColorStop(0, `rgba(${base.r}, ${base.g}, ${base.b}, 0.35)`);
      gradient.addColorStop(
        1,
        `rgba(${Math.min(255, base.r + 35)}, ${Math.min(255, base.g + 30)}, ${Math.min(255, base.b + 30)}, 0.05)`
      );
      context.fillStyle = gradient;
      context.fillRect(0, 0, width, height);

      for (let y = -density; y < height + density; y += density) {
        context.beginPath();
        for (let x = -density; x <= width + density; x += 6) {
          const wave =
            Math.sin(x * 0.02 + t + y * 0.04) * (8 + noiseIntensity * 3);
          const wave2 =
            Math.cos(x * 0.011 - t * 1.4 + y * 0.02) * (5 + scale * 2.5);
          const py = y + wave + wave2;
          if (x === -density) {
            context.moveTo(x, py);
          } else {
            context.lineTo(x, py);
          }
        }

        const lum = 0.6 + 0.4 * Math.sin(t + y * 0.02);
        const r = Math.min(255, Math.floor(base.r * lum + 35));
        const g = Math.min(255, Math.floor(base.g * lum + 35));
        const b = Math.min(255, Math.floor(base.b * lum + 45));
        context.strokeStyle = `rgba(${r}, ${g}, ${b}, ${alpha})`;
        context.lineWidth = 1.2;
        context.stroke();
      }

      context.restore();
      rafId = requestAnimationFrame(draw);
    };

    resize();
    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(parent);
    rafId = requestAnimationFrame(draw);

    return () => {
      cancelAnimationFrame(rafId);
      resizeObserver.disconnect();
    };
  }, [color, noiseIntensity, rotation, scale, speed]);

  return (
    <div className={styles.silkLegacy} aria-hidden>
      <canvas ref={canvasReference} />
    </div>
  );
};

export default SilkLegacy;
