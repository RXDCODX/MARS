import { CSSProperties, useEffect, useRef } from "react";

import styles from "./GridScanLegacy.module.scss";

type GridScanLegacyProperties = {
  lineThickness?: number;
  linesColor?: string;
  scanColor?: string;
  scanOpacity?: number;
  gridScale?: number;
  noiseIntensity?: number;
  className?: string;
  style?: CSSProperties;
};

const GridScanLegacy: React.FC<GridScanLegacyProperties> = ({
  lineThickness = 1,
  linesColor = "#392e4e",
  scanColor = "#FF9FFC",
  scanOpacity = 0.4,
  gridScale = 0.1,
  noiseIntensity = 0.01,
  className,
  style,
}) => {
  const canvasReference = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;

    const context = canvas.getContext("2d", { alpha: true });
    if (!context) return;

    const parent = canvas.parentElement;
    if (!parent) return;

    let width = 1;
    let height = 1;
    let rafId = 0;

    const hexToRgba = (hex: string, alpha: number) => {
      const clean = hex.replace("#", "");
      const expanded =
        clean.length === 3
          ? clean
              .split("")
              .map(ch => `${ch}${ch}`)
              .join("")
          : clean;
      const r = Number.parseInt(expanded.slice(0, 2), 16);
      const g = Number.parseInt(expanded.slice(2, 4), 16);
      const b = Number.parseInt(expanded.slice(4, 6), 16);

      if ([r, g, b].some(Number.isNaN)) {
        return `rgba(255, 159, 252, ${alpha})`;
      }

      return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    };

    const resize = () => {
      width = Math.max(1, Math.floor(parent.clientWidth));
      height = Math.max(1, Math.floor(parent.clientHeight));
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(width * dpr);
      canvas.height = Math.floor(height * dpr);
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const drawGrid = (t: number) => {
      const spacing = Math.max(14, Math.floor(56 * Math.max(0.05, gridScale)));
      const centerY = height * 0.58;
      const horizon = height * 0.2;
      const offset = (t * 35) % spacing;

      context.lineWidth = Math.max(0.6, lineThickness);
      context.strokeStyle = linesColor;
      context.globalAlpha = 0.45;

      for (let y = centerY; y > horizon; y -= spacing) {
        const ratio = (centerY - y) / Math.max(1, centerY - horizon);
        const spread = width * (0.1 + ratio * 0.9);
        context.beginPath();
        context.moveTo((width - spread) / 2, y + offset * ratio);
        context.lineTo((width + spread) / 2, y + offset * ratio);
        context.stroke();
      }

      const columns = Math.ceil(width / spacing) + 6;
      for (let index = -columns; index <= columns; index += 1) {
        const x = width / 2 + index * spacing;
        context.beginPath();
        context.moveTo(x, centerY);
        context.lineTo(width / 2 + (x - width / 2) * 2.2, horizon);
        context.stroke();
      }
    };

    const drawScan = (t: number) => {
      const cycle = 4;
      const phase = (t % cycle) / cycle;
      const y = height * 0.18 + phase * height * 0.65;
      const scanGradient = context.createLinearGradient(0, y - 35, 0, y + 35);
      scanGradient.addColorStop(0, "rgba(0, 0, 0, 0)");
      scanGradient.addColorStop(
        0.5,
        hexToRgba(scanColor, Math.max(0, Math.min(1, scanOpacity)))
      );
      scanGradient.addColorStop(1, "rgba(0, 0, 0, 0)");

      context.globalAlpha = 1;
      context.fillStyle = scanGradient;
      context.fillRect(0, y - 35, width, 70);

      context.globalAlpha = 0.35;
      context.strokeStyle = scanColor;
      context.lineWidth = 1;
      context.beginPath();
      context.moveTo(0, y);
      context.lineTo(width, y);
      context.stroke();
    };

    const drawNoise = () => {
      const noise = Math.max(0, Math.min(0.12, noiseIntensity * 0.8));
      if (noise <= 0) return;
      context.globalAlpha = noise;
      for (let index = 0; index < 140; index += 1) {
        const x = Math.random() * width;
        const y = Math.random() * height;
        const size = Math.random() * 2 + 0.5;
        context.fillStyle = "#ffffff";
        context.fillRect(x, y, size, size);
      }
    };

    const loop = (timeMs: number) => {
      const t = timeMs / 1000;
      context.clearRect(0, 0, width, height);

      const bg = context.createLinearGradient(0, 0, 0, height);
      bg.addColorStop(0, "rgba(3, 3, 8, 0.6)");
      bg.addColorStop(1, "rgba(0, 0, 0, 0.95)");
      context.globalAlpha = 1;
      context.fillStyle = bg;
      context.fillRect(0, 0, width, height);

      drawGrid(t);
      drawScan(t);
      drawNoise();

      rafId = requestAnimationFrame(loop);
    };

    resize();
    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(parent);
    rafId = requestAnimationFrame(loop);

    return () => {
      cancelAnimationFrame(rafId);
      resizeObserver.disconnect();
    };
  }, [
    lineThickness,
    linesColor,
    scanColor,
    scanOpacity,
    gridScale,
    noiseIntensity,
  ]);

  return (
    <div
      className={`${styles.gridscanLegacy}${className ? ` ${className}` : ""}`}
      style={style}
      aria-hidden
    >
      <canvas ref={canvasReference} />
    </div>
  );
};

export default GridScanLegacy;
