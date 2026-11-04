import "./ShapeGrid.css";

import React, { useEffect, useRef } from "react";

type CanvasStrokeStyle = string | CanvasGradient | CanvasPattern;

interface GridOffset {
  x: number;
  y: number;
}

interface ShapeGridProperties {
  direction?: "diagonal" | "up" | "right" | "down" | "left";
  speed?: number;
  borderColor?: CanvasStrokeStyle;
  squareSize?: number;
  hoverFillColor?: CanvasStrokeStyle;
  shape?: "square" | "hexagon" | "circle" | "triangle";
  hoverTrailAmount?: number;
}

const ShapeGrid: React.FC<ShapeGridProperties> = ({
  direction = "right",
  speed = 1,
  borderColor = "#999",
  squareSize = 40,
  hoverFillColor = "#222",
  shape = "square",
  hoverTrailAmount = 0,
}) => {
  const canvasReference = useRef<HTMLCanvasElement>(null);
  const requestReference = useRef<number | null>(null);
  const numberSquaresX = useRef<number>(0);
  const numberSquaresY = useRef<number>(0);
  const gridOffset = useRef<GridOffset>({ x: 0, y: 0 });
  const hoveredSquareReference = useRef<GridOffset | null>(null);
  const trailCells = useRef<GridOffset[]>([]);
  const cellOpacities = useRef<Map<string, number>>(new Map());

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;
    const context = canvas.getContext("2d");

    const isHex = shape === "hexagon";
    const isTri = shape === "triangle";
    const hexHoriz = squareSize * 1.5;
    const hexVert = squareSize * Math.sqrt(3);

    const resizeCanvas = () => {
      canvas.width = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
      numberSquaresX.current = Math.ceil(canvas.width / squareSize) + 1;
      numberSquaresY.current = Math.ceil(canvas.height / squareSize) + 1;
    };

    window.addEventListener("resize", resizeCanvas);
    resizeCanvas();

    const drawHex = (cx: number, cy: number, size: number) => {
      if (!context) return;
      context.beginPath();
      for (let index = 0; index < 6; index++) {
        const angle = (Math.PI / 3) * index;
        const vx = cx + size * Math.cos(angle);
        const vy = cy + size * Math.sin(angle);
        if (index === 0) context.moveTo(vx, vy);
        else context.lineTo(vx, vy);
      }
      context.closePath();
    };

    const drawCircle = (cx: number, cy: number, size: number) => {
      if (!context) return;
      context.beginPath();
      context.arc(cx, cy, size / 2, 0, Math.PI * 2);
      context.closePath();
    };

    const drawTriangle = (
      cx: number,
      cy: number,
      size: number,
      flip: boolean
    ) => {
      if (!context) return;
      context.beginPath();
      if (flip) {
        context.moveTo(cx, cy + size / 2);
        context.lineTo(cx + size / 2, cy - size / 2);
        context.lineTo(cx - size / 2, cy - size / 2);
      } else {
        context.moveTo(cx, cy - size / 2);
        context.lineTo(cx + size / 2, cy + size / 2);
        context.lineTo(cx - size / 2, cy + size / 2);
      }
      context.closePath();
    };

    const drawGrid = () => {
      if (!context) return;
      context.clearRect(0, 0, canvas.width, canvas.height);

      if (isHex) {
        const colShift = Math.floor(gridOffset.current.x / hexHoriz);
        const offsetX =
          ((gridOffset.current.x % hexHoriz) + hexHoriz) % hexHoriz;
        const offsetY = ((gridOffset.current.y % hexVert) + hexVert) % hexVert;

        const cols = Math.ceil(canvas.width / hexHoriz) + 3;
        const rows = Math.ceil(canvas.height / hexVert) + 3;

        for (let col = -2; col < cols; col++) {
          for (let row = -2; row < rows; row++) {
            const cx = col * hexHoriz + offsetX;
            const cy =
              row * hexVert +
              ((col + colShift) % 2 === 0 ? 0 : hexVert / 2) +
              offsetY;

            const cellKey = `${col},${row}`;
            const alpha = cellOpacities.current.get(cellKey);
            if (alpha) {
              context.globalAlpha = alpha;
              drawHex(cx, cy, squareSize);
              context.fillStyle = hoverFillColor;
              context.fill();
              context.globalAlpha = 1;
            }

            drawHex(cx, cy, squareSize);
            context.strokeStyle = borderColor;
            context.stroke();
          }
        }
      } else if (isTri) {
        const halfW = squareSize / 2;
        const colShift = Math.floor(gridOffset.current.x / halfW);
        const rowShift = Math.floor(gridOffset.current.y / squareSize);
        const offsetX = ((gridOffset.current.x % halfW) + halfW) % halfW;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const cols = Math.ceil(canvas.width / halfW) + 4;
        const rows = Math.ceil(canvas.height / squareSize) + 4;

        for (let col = -2; col < cols; col++) {
          for (let row = -2; row < rows; row++) {
            const cx = col * halfW + offsetX;
            const cy = row * squareSize + squareSize / 2 + offsetY;
            const isFlip =
              (((col + colShift + row + rowShift) % 2) + 2) % 2 !== 0;

            const cellKey = `${col},${row}`;
            const alpha = cellOpacities.current.get(cellKey);
            if (alpha) {
              context.globalAlpha = alpha;
              drawTriangle(cx, cy, squareSize, isFlip);
              context.fillStyle = hoverFillColor;
              context.fill();
              context.globalAlpha = 1;
            }

            drawTriangle(cx, cy, squareSize, isFlip);
            context.strokeStyle = borderColor;
            context.stroke();
          }
        }
      } else if (shape === "circle") {
        const offsetX =
          ((gridOffset.current.x % squareSize) + squareSize) % squareSize;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const cols = Math.ceil(canvas.width / squareSize) + 3;
        const rows = Math.ceil(canvas.height / squareSize) + 3;

        for (let col = -2; col < cols; col++) {
          for (let row = -2; row < rows; row++) {
            const cx = col * squareSize + squareSize / 2 + offsetX;
            const cy = row * squareSize + squareSize / 2 + offsetY;

            const cellKey = `${col},${row}`;
            const alpha = cellOpacities.current.get(cellKey);
            if (alpha) {
              context.globalAlpha = alpha;
              drawCircle(cx, cy, squareSize);
              context.fillStyle = hoverFillColor;
              context.fill();
              context.globalAlpha = 1;
            }

            drawCircle(cx, cy, squareSize);
            context.strokeStyle = borderColor;
            context.stroke();
          }
        }
      } else {
        const offsetX =
          ((gridOffset.current.x % squareSize) + squareSize) % squareSize;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const cols = Math.ceil(canvas.width / squareSize) + 3;
        const rows = Math.ceil(canvas.height / squareSize) + 3;

        for (let col = -2; col < cols; col++) {
          for (let row = -2; row < rows; row++) {
            const sx = col * squareSize + offsetX;
            const sy = row * squareSize + offsetY;

            const cellKey = `${col},${row}`;
            const alpha = cellOpacities.current.get(cellKey);
            if (alpha) {
              context.globalAlpha = alpha;
              context.fillStyle = hoverFillColor;
              context.fillRect(sx, sy, squareSize, squareSize);
              context.globalAlpha = 1;
            }

            context.strokeStyle = borderColor;
            context.strokeRect(sx, sy, squareSize, squareSize);
          }
        }
      }

      const gradient = context.createRadialGradient(
        canvas.width / 2,
        canvas.height / 2,
        0,
        canvas.width / 2,
        canvas.height / 2,
        Math.hypot(canvas.width, canvas.height) / 2
      );
      gradient.addColorStop(0, "rgba(0, 0, 0, 0)");
      gradient.addColorStop(1, "#060010");

      context.fillStyle = gradient;
      context.fillRect(0, 0, canvas.width, canvas.height);
    };

    const updateAnimation = () => {
      const effectiveSpeed = Math.max(speed, 0.1);
      const wrapX = isHex ? hexHoriz * 2 : squareSize;
      const wrapY = isHex ? hexVert : isTri ? squareSize * 2 : squareSize;

      switch (direction) {
        case "right": {
          gridOffset.current.x =
            (gridOffset.current.x - effectiveSpeed + wrapX) % wrapX;
          break;
        }
        case "left": {
          gridOffset.current.x =
            (gridOffset.current.x + effectiveSpeed + wrapX) % wrapX;
          break;
        }
        case "up": {
          gridOffset.current.y =
            (gridOffset.current.y + effectiveSpeed + wrapY) % wrapY;
          break;
        }
        case "down": {
          gridOffset.current.y =
            (gridOffset.current.y - effectiveSpeed + wrapY) % wrapY;
          break;
        }
        case "diagonal": {
          gridOffset.current.x =
            (gridOffset.current.x - effectiveSpeed + wrapX) % wrapX;
          gridOffset.current.y =
            (gridOffset.current.y - effectiveSpeed + wrapY) % wrapY;
          break;
        }
        default: {
          break;
        }
      }

      updateCellOpacities();
      drawGrid();
      requestReference.current = requestAnimationFrame(updateAnimation);
    };

    const updateCellOpacities = () => {
      const targets = new Map<string, number>();

      if (hoveredSquareReference.current) {
        targets.set(
          `${hoveredSquareReference.current.x},${hoveredSquareReference.current.y}`,
          1
        );
      }

      if (hoverTrailAmount > 0) {
        for (let index = 0; index < trailCells.current.length; index++) {
          const t = trailCells.current[index];
          const key = `${t.x},${t.y}`;
          if (!targets.has(key)) {
            targets.set(
              key,
              (trailCells.current.length - index) /
                (trailCells.current.length + 1)
            );
          }
        }
      }

      for (const [key] of targets) {
        if (!cellOpacities.current.has(key)) {
          cellOpacities.current.set(key, 0);
        }
      }

      for (const [key, opacity] of cellOpacities.current) {
        const target = targets.get(key) || 0;
        const next = opacity + (target - opacity) * 0.15;
        if (next < 0.005) {
          cellOpacities.current.delete(key);
        } else {
          cellOpacities.current.set(key, next);
        }
      }
    };

    const handleMouseMove = (event: MouseEvent) => {
      const rect = canvas.getBoundingClientRect();
      const mouseX = event.clientX - rect.left;
      const mouseY = event.clientY - rect.top;

      if (isHex) {
        const colShift = Math.floor(gridOffset.current.x / hexHoriz);
        const offsetX =
          ((gridOffset.current.x % hexHoriz) + hexHoriz) % hexHoriz;
        const offsetY = ((gridOffset.current.y % hexVert) + hexVert) % hexVert;
        const adjustedX = mouseX - offsetX;
        const adjustedY = mouseY - offsetY;

        const col = Math.round(adjustedX / hexHoriz);
        const rowOffset = (col + colShift) % 2 === 0 ? 0 : hexVert / 2;
        const row = Math.round((adjustedY - rowOffset) / hexVert);

        if (
          !hoveredSquareReference.current ||
          hoveredSquareReference.current.x !== col ||
          hoveredSquareReference.current.y !== row
        ) {
          if (hoveredSquareReference.current && hoverTrailAmount > 0) {
            trailCells.current.unshift({ ...hoveredSquareReference.current });
            if (trailCells.current.length > hoverTrailAmount)
              trailCells.current.length = hoverTrailAmount;
          }
          hoveredSquareReference.current = { x: col, y: row };
        }
      } else if (isTri) {
        const halfW = squareSize / 2;
        const offsetX = ((gridOffset.current.x % halfW) + halfW) % halfW;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const adjustedX = mouseX - offsetX;
        const adjustedY = mouseY - offsetY;

        const col = Math.round(adjustedX / halfW);
        const row = Math.floor(adjustedY / squareSize);

        if (
          !hoveredSquareReference.current ||
          hoveredSquareReference.current.x !== col ||
          hoveredSquareReference.current.y !== row
        ) {
          if (hoveredSquareReference.current && hoverTrailAmount > 0) {
            trailCells.current.unshift({ ...hoveredSquareReference.current });
            if (trailCells.current.length > hoverTrailAmount)
              trailCells.current.length = hoverTrailAmount;
          }
          hoveredSquareReference.current = { x: col, y: row };
        }
      } else if (shape === "circle") {
        const offsetX =
          ((gridOffset.current.x % squareSize) + squareSize) % squareSize;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const adjustedX = mouseX - offsetX;
        const adjustedY = mouseY - offsetY;

        const col = Math.round(adjustedX / squareSize);
        const row = Math.round(adjustedY / squareSize);

        if (
          !hoveredSquareReference.current ||
          hoveredSquareReference.current.x !== col ||
          hoveredSquareReference.current.y !== row
        ) {
          if (hoveredSquareReference.current && hoverTrailAmount > 0) {
            trailCells.current.unshift({ ...hoveredSquareReference.current });
            if (trailCells.current.length > hoverTrailAmount)
              trailCells.current.length = hoverTrailAmount;
          }
          hoveredSquareReference.current = { x: col, y: row };
        }
      } else {
        const offsetX =
          ((gridOffset.current.x % squareSize) + squareSize) % squareSize;
        const offsetY =
          ((gridOffset.current.y % squareSize) + squareSize) % squareSize;

        const adjustedX = mouseX - offsetX;
        const adjustedY = mouseY - offsetY;

        const col = Math.floor(adjustedX / squareSize);
        const row = Math.floor(adjustedY / squareSize);

        if (
          !hoveredSquareReference.current ||
          hoveredSquareReference.current.x !== col ||
          hoveredSquareReference.current.y !== row
        ) {
          if (hoveredSquareReference.current && hoverTrailAmount > 0) {
            trailCells.current.unshift({ ...hoveredSquareReference.current });
            if (trailCells.current.length > hoverTrailAmount)
              trailCells.current.length = hoverTrailAmount;
          }
          hoveredSquareReference.current = { x: col, y: row };
        }
      }
    };

    const handleMouseLeave = () => {
      if (hoveredSquareReference.current && hoverTrailAmount > 0) {
        trailCells.current.unshift({ ...hoveredSquareReference.current });
        if (trailCells.current.length > hoverTrailAmount)
          trailCells.current.length = hoverTrailAmount;
      }
      hoveredSquareReference.current = null;
    };

    canvas.addEventListener("mousemove", handleMouseMove);
    canvas.addEventListener("mouseleave", handleMouseLeave);
    requestReference.current = requestAnimationFrame(updateAnimation);

    return () => {
      window.removeEventListener("resize", resizeCanvas);
      if (requestReference.current)
        cancelAnimationFrame(requestReference.current);
      canvas.removeEventListener("mousemove", handleMouseMove);
      canvas.removeEventListener("mouseleave", handleMouseLeave);
    };
  }, [
    direction,
    speed,
    borderColor,
    hoverFillColor,
    squareSize,
    shape,
    hoverTrailAmount,
  ]);

  return <canvas ref={canvasReference} className="shapegrid-canvas"></canvas>;
};

export default ShapeGrid;
