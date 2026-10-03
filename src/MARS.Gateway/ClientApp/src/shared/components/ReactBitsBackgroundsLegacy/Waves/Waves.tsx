import "./Waves.css";

import React, { CSSProperties, useEffect, useRef } from "react";

class Grad {
  x: number;
  y: number;
  z: number;
  constructor(x: number, y: number, z: number) {
    this.x = x;
    this.y = y;
    this.z = z;
  }
  dot2(x: number, y: number): number {
    return this.x * x + this.y * y;
  }
}

class Noise {
  grad3: Grad[];
  p: number[];
  perm: number[];
  gradP: Grad[];

  constructor(seed = 0) {
    this.grad3 = [
      new Grad(1, 1, 0),
      new Grad(-1, 1, 0),
      new Grad(1, -1, 0),
      new Grad(-1, -1, 0),
      new Grad(1, 0, 1),
      new Grad(-1, 0, 1),
      new Grad(1, 0, -1),
      new Grad(-1, 0, -1),
      new Grad(0, 1, 1),
      new Grad(0, -1, 1),
      new Grad(0, 1, -1),
      new Grad(0, -1, -1),
    ];
    this.p = [
      151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
      140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247,
      120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177,
      33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165,
      71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211,
      133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25,
      63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
      135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217,
      226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206,
      59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248,
      152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22,
      39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218,
      246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
      81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
      184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
      222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
    ];
    this.perm = Array.from({ length: 512 });
    this.gradP = Array.from({ length: 512 });
    this.seed(seed);
  }
  seed(seed: number) {
    if (seed > 0 && seed < 1) seed *= 65_536;
    seed = Math.floor(seed);
    if (seed < 256) seed |= seed << 8;
    for (let index = 0; index < 256; index++) {
      const v =
        index & 1
          ? this.p[index] ^ (seed & 255)
          : this.p[index] ^ ((seed >> 8) & 255);
      this.perm[index] = this.perm[index + 256] = v;
      this.gradP[index] = this.gradP[index + 256] = this.grad3[v % 12];
    }
  }
  fade(t: number): number {
    return t * t * t * (t * (t * 6 - 15) + 10);
  }
  lerp(a: number, b: number, t: number): number {
    return (1 - t) * a + t * b;
  }
  perlin2(x: number, y: number): number {
    let X = Math.floor(x),
      Y = Math.floor(y);
    x -= X;
    y -= Y;
    X &= 255;
    Y &= 255;
    const n00 = this.gradP[X + this.perm[Y]].dot2(x, y);
    const n01 = this.gradP[X + this.perm[Y + 1]].dot2(x, y - 1);
    const n10 = this.gradP[X + 1 + this.perm[Y]].dot2(x - 1, y);
    const n11 = this.gradP[X + 1 + this.perm[Y + 1]].dot2(x - 1, y - 1);
    const u = this.fade(x);
    return this.lerp(
      this.lerp(n00, n10, u),
      this.lerp(n01, n11, u),
      this.fade(y)
    );
  }
}

interface Point {
  x: number;
  y: number;
  wave: { x: number; y: number };
  cursor: { x: number; y: number; vx: number; vy: number };
}

interface Mouse {
  x: number;
  y: number;
  lx: number;
  ly: number;
  sx: number;
  sy: number;
  v: number;
  vs: number;
  a: number;
  set: boolean;
}

interface Config {
  lineColor: string;
  waveSpeedX: number;
  waveSpeedY: number;
  waveAmpX: number;
  waveAmpY: number;
  friction: number;
  tension: number;
  maxCursorMove: number;
  xGap: number;
  yGap: number;
}

interface WavesProperties {
  lineColor?: string;
  backgroundColor?: string;
  waveSpeedX?: number;
  waveSpeedY?: number;
  waveAmpX?: number;
  waveAmpY?: number;
  xGap?: number;
  yGap?: number;
  friction?: number;
  tension?: number;
  maxCursorMove?: number;
  style?: CSSProperties;
  className?: string;
}

const Waves: React.FC<WavesProperties> = ({
  lineColor = "black",
  backgroundColor = "transparent",
  waveSpeedX = 0.0125,
  waveSpeedY = 0.005,
  waveAmpX = 32,
  waveAmpY = 16,
  xGap = 10,
  yGap = 32,
  friction = 0.925,
  tension = 0.005,
  maxCursorMove = 100,
  style = {},
  className = "",
}) => {
  const containerReference = useRef<HTMLDivElement>(null);
  const canvasReference = useRef<HTMLCanvasElement>(null);
  const contextReference = useRef<CanvasRenderingContext2D | null>(null);
  const boundingReference = useRef<{
    width: number;
    height: number;
    left: number;
    top: number;
  }>({
    width: 0,
    height: 0,
    left: 0,
    top: 0,
  });
  const noiseReference = useRef(new Noise(Math.random()));
  const linesReference = useRef<Point[][]>([]);
  const mouseReference = useRef<Mouse>({
    x: -10,
    y: 0,
    lx: 0,
    ly: 0,
    sx: 0,
    sy: 0,
    v: 0,
    vs: 0,
    a: 0,
    set: false,
  });
  const configReference = useRef<Config>({
    lineColor,
    waveSpeedX,
    waveSpeedY,
    waveAmpX,
    waveAmpY,
    friction,
    tension,
    maxCursorMove,
    xGap,
    yGap,
  });
  const frameIdReference = useRef<number | null>(null);

  useEffect(() => {
    configReference.current = {
      lineColor,
      waveSpeedX,
      waveSpeedY,
      waveAmpX,
      waveAmpY,
      friction,
      tension,
      maxCursorMove,
      xGap,
      yGap,
    };
  }, [
    lineColor,
    waveSpeedX,
    waveSpeedY,
    waveAmpX,
    waveAmpY,
    friction,
    tension,
    maxCursorMove,
    xGap,
    yGap,
  ]);

  useEffect(() => {
    const canvas = canvasReference.current;
    const container = containerReference.current;
    if (!canvas || !container) return;
    contextReference.current = canvas.getContext("2d");

    function setSize() {
      if (!container || !canvas) return;
      const rect = container.getBoundingClientRect();
      boundingReference.current = {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top,
      };
      canvas.width = rect.width;
      canvas.height = rect.height;
    }

    function setLines() {
      const { width, height } = boundingReference.current;
      linesReference.current = [];
      const oWidth = width + 200,
        oHeight = height + 30;
      const { xGap, yGap } = configReference.current;
      const totalLines = Math.ceil(oWidth / xGap);
      const totalPoints = Math.ceil(oHeight / yGap);
      const xStart = (width - xGap * totalLines) / 2;
      const yStart = (height - yGap * totalPoints) / 2;
      for (let index = 0; index <= totalLines; index++) {
        const pts: Point[] = [];
        for (let index_ = 0; index_ <= totalPoints; index_++) {
          pts.push({
            x: xStart + xGap * index,
            y: yStart + yGap * index_,
            wave: { x: 0, y: 0 },
            cursor: { x: 0, y: 0, vx: 0, vy: 0 },
          });
        }
        linesReference.current.push(pts);
      }
    }

    function movePoints(time: number) {
      const lines = linesReference.current;
      const mouse = mouseReference.current;
      const noise = noiseReference.current;
      const {
        waveSpeedX,
        waveSpeedY,
        waveAmpX,
        waveAmpY,
        friction,
        tension,
        maxCursorMove,
      } = configReference.current;
      lines.forEach(pts => {
        pts.forEach(p => {
          const move =
            noise.perlin2(
              (p.x + time * waveSpeedX) * 0.002,
              (p.y + time * waveSpeedY) * 0.0015
            ) * 12;
          p.wave.x = Math.cos(move) * waveAmpX;
          p.wave.y = Math.sin(move) * waveAmpY;

          const dx = p.x - mouse.sx,
            dy = p.y - mouse.sy;
          const distribution = Math.hypot(dx, dy);
          const l = Math.max(175, mouse.vs);
          if (distribution < l) {
            const s = 1 - distribution / l;
            const f = Math.cos(distribution * 0.001) * s;
            p.cursor.vx += Math.cos(mouse.a) * f * l * mouse.vs * 0.00065;
            p.cursor.vy += Math.sin(mouse.a) * f * l * mouse.vs * 0.00065;
          }

          p.cursor.vx += (0 - p.cursor.x) * tension;
          p.cursor.vy += (0 - p.cursor.y) * tension;
          p.cursor.vx *= friction;
          p.cursor.vy *= friction;
          p.cursor.x += p.cursor.vx * 2;
          p.cursor.y += p.cursor.vy * 2;
          p.cursor.x = Math.min(
            maxCursorMove,
            Math.max(-maxCursorMove, p.cursor.x)
          );
          p.cursor.y = Math.min(
            maxCursorMove,
            Math.max(-maxCursorMove, p.cursor.y)
          );
        });
      });
    }

    function moved(point: Point, withCursor = true): { x: number; y: number } {
      const x = point.x + point.wave.x + (withCursor ? point.cursor.x : 0);
      const y = point.y + point.wave.y + (withCursor ? point.cursor.y : 0);
      return { x: Math.round(x * 10) / 10, y: Math.round(y * 10) / 10 };
    }

    function drawLines() {
      const { width, height } = boundingReference.current;
      const context = contextReference.current;
      if (!context) return;
      context.clearRect(0, 0, width, height);
      context.beginPath();
      context.strokeStyle = configReference.current.lineColor;
      linesReference.current.forEach(points => {
        let p1 = moved(points[0], false);
        context.moveTo(p1.x, p1.y);
        points.forEach((p, index) => {
          const isLast = index === points.length - 1;
          p1 = moved(p, !isLast);
          const p2 = moved(points[index + 1] || points.at(-1), !isLast);
          context.lineTo(p1.x, p1.y);
          if (isLast) context.moveTo(p2.x, p2.y);
        });
      });
      context.stroke();
    }

    function tick(t: number) {
      if (!container) return;
      const mouse = mouseReference.current;
      mouse.sx += (mouse.x - mouse.sx) * 0.1;
      mouse.sy += (mouse.y - mouse.sy) * 0.1;
      const dx = mouse.x - mouse.lx,
        dy = mouse.y - mouse.ly;
      const d = Math.hypot(dx, dy);
      mouse.v = d;
      mouse.vs += (d - mouse.vs) * 0.1;
      mouse.vs = Math.min(100, mouse.vs);
      mouse.lx = mouse.x;
      mouse.ly = mouse.y;
      mouse.a = Math.atan2(dy, dx);
      container.style.setProperty("--x", `${mouse.sx}px`);
      container.style.setProperty("--y", `${mouse.sy}px`);

      movePoints(t);
      drawLines();
      frameIdReference.current = requestAnimationFrame(tick);
    }

    function onResize() {
      setSize();
      setLines();
    }
    function onMouseMove(e: MouseEvent) {
      updateMouse(e.clientX, e.clientY);
    }
    function onTouchMove(e: TouchEvent) {
      const touch = e.touches[0];
      updateMouse(touch.clientX, touch.clientY);
    }
    function updateMouse(x: number, y: number) {
      const mouse = mouseReference.current;
      const b = boundingReference.current;
      mouse.x = x - b.left;
      mouse.y = y - b.top;
      if (!mouse.set) {
        mouse.sx = mouse.x;
        mouse.sy = mouse.y;
        mouse.lx = mouse.x;
        mouse.ly = mouse.y;
        mouse.set = true;
      }
    }

    setSize();
    setLines();
    frameIdReference.current = requestAnimationFrame(tick);
    window.addEventListener("resize", onResize);
    globalThis.addEventListener("mousemove", onMouseMove);
    globalThis.addEventListener("touchmove", onTouchMove, { passive: false });

    return () => {
      window.removeEventListener("resize", onResize);
      globalThis.removeEventListener("mousemove", onMouseMove);
      globalThis.removeEventListener("touchmove", onTouchMove);
      if (frameIdReference.current !== null) {
        cancelAnimationFrame(frameIdReference.current);
      }
    };
  }, []);

  return (
    <div
      ref={containerReference}
      className={`waves ${className}`}
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        margin: 0,
        padding: 0,
        width: "100%",
        height: "100%",
        overflow: "hidden",
        backgroundColor,
        ...style,
      }}
    >
      <canvas ref={canvasReference} className="waves-canvas" />
    </div>
  );
};

export default Waves;
