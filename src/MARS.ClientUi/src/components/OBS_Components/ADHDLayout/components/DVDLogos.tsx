import { useEffect, useRef } from "react";

interface DVDLogo {
  id: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  color: string;
}

// Максимальное количество DVD элементов
const DVD_COUNT_LIMIT = 20;
const DEFAULT_DVD_COUNT = 12;
const MAX_SPEED = 4;
const MIN_SPEED = 2;

const colors = [
  "#00ffff",
  "#f0ffff",
  "#f5f5dc",
  "#000000",
  "#0000ff",
  "#a52a2a",
  "#00ffff",
  "#00008b",
  "#008b8b",
  "#a9a9a9",
  "#006400",
  "#bdb76b",
  "#8b008b",
  "#556b2f",
  "#ff8c00",
  "#9932cc",
  "#8b0000",
  "#e9967a",
  "#9400d3",
  "#ff00ff",
  "#ffd700",
  "#008000",
  "#4b0082",
  "#f0e68c",
  "#add8e6",
  "#e0ffff",
  "#90ee90",
  "#d3d3d3",
  "#ffb6c1",
  "#ffffe0",
  "#00ff00",
  "#ff00ff",
  "#800000",
  "#000080",
  "#808000",
  "#ffa500",
  "#ffc0cb",
  "#800080",
  "#ff0000",
  "#c0c0c0",
  "#ffffff",
  "#ffff00",
];

// Функция для получения случайного неповторяющегося цвета
const getRandomUniqueColors = (count: number) => {
  const shuffled = [...colors].sort(() => 0.5 - Math.random());
  return shuffled.slice(0, count);
};

// Функция для получения случайного цвета
const getRandomColor = () => colors[Math.floor(Math.random() * colors.length)];

interface DVDLogosProperties {
  count?: number;
}

export function DVDLogos({ count = DEFAULT_DVD_COUNT }: DVDLogosProperties) {
  const canvasReference = useRef<HTMLCanvasElement>(null);
  const animationReference = useRef<number | undefined>(undefined);
  const logosReference = useRef<DVDLogo[]>([]);

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;

    const context = canvas.getContext("2d");
    if (!context) return;

    const dvdCount = Math.min(DVD_COUNT_LIMIT, Math.max(1, Math.round(count)));

    // Устанавливаем размер canvas равным размеру окна
    const resizeCanvas = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };

    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);

    // Получаем случайные неповторяющиеся цвета
    const initialColors = getRandomUniqueColors(dvdCount);

    // Инициализация DVD логотипов
    logosReference.current = Array.from({ length: dvdCount }, (_, index) => ({
      id: index + 1,
      x: 100 + index * 100,
      y: 100 + index * 50,
      vx: (Math.random() - 0.5) * (MAX_SPEED - MIN_SPEED) + MIN_SPEED,
      vy: (Math.random() - 0.5) * (MAX_SPEED - MIN_SPEED) + MIN_SPEED,
      color: initialColors[index],
    }));

    // Создаем SVG элемент для парсинга (из примера)
    const svgString = `<svg xmlns="http://www.w3.org/2000/svg" id="image" viewBox="0 0 67.417 29.523">
      <path d="M59.646 0H43.07l-8.926 10.291L30.501 0H5.911L5 3.825h9.017c2.915 0 6.302 1.34 5.647 4.371-.729 3.37-4.19 5.01-10.474 5.01L11.011 5.1H4.727L2.04 16.758h8.516c5.601 0 13.115-1.64 15.165-7.969.551-1.702.496-3.225.11-4.508l.026-.001 5.738 16.395L46.35 3.825h9.381c2.915 0 5.618 1.33 5.01 4.371-.547 2.732-3.552 5.01-9.837 5.01L52.725 5.1H46.44l-2.687 11.658h6.968c5.601 0 14.299-1.64 16.348-7.969C68.764 3.555 63.927 0 59.646 0z"/>
      <ellipse cx="31.686" cy="25.319" rx="31.686" ry="4.204"/>
      <ellipse cx="48.558" cy="25.114" rx="3.671" ry="2.608" fill="#fff"/>
      <ellipse cx="48.558" cy="25.114" rx="2.397" ry="1.516"/>
      <path fill="#fff" d="M22.896 22.655h1.435v5.01h-1.435zM12.833 22.655h1.685l1.844 3.097 1.844-3.097h1.685l-2.983 5.01h-1.093zM30.501 22.657c2.045 0 3.703 1.156 3.703 2.525s-1.658 2.479-3.703 2.479h-1.958v-5.007l1.958.003z"/>
      <path d="M30.501 26.563c1.245 0 2.254-.58 2.254-1.381 0-.8-1.009-1.427-2.254-1.427h-.546v2.816l.546-.008z"/>
      <path fill="#fff" d="M41.727 22.655h-4.155v5.01h4.155v-1.093h-2.721l-.002-.986h2.722v-1.028h-2.722l.002-.81h2.721z"/>
    </svg>`;

    const parser = new DOMParser();
    const svgDocument = parser.parseFromString(svgString, "image/svg+xml");
    const svg = svgDocument.documentElement;

    const animate = () => {
      // Очищаем canvas
      context.clearRect(0, 0, canvas.width, canvas.height);

      // Обновляем позиции и отрисовываем логотипы
      logosReference.current.forEach(logo => {
        // Обновляем позицию
        const newX = logo.x + logo.vx;
        const newY = logo.y + logo.vy;

        // Проверка отскока от краев экрана
        if (newX <= 0 || newX >= canvas.width - 100) {
          logo.vx = -logo.vx;
          logo.color = getRandomColor();
        }
        if (newY <= 0 || newY >= canvas.height - 100) {
          logo.vy = -logo.vy;
          logo.color = getRandomColor();
        }

        // Обновляем позицию
        logo.x = newX;
        logo.y = newY;

        // Рисуем DVD логотип
        drawDVDLogo(context, logo, svg);
      });

      animationReference.current = requestAnimationFrame(animate);
    };

    animationReference.current = requestAnimationFrame(animate);

    return () => {
      window.removeEventListener("resize", resizeCanvas);
      if (animationReference.current) {
        cancelAnimationFrame(animationReference.current);
      }
    };
  }, [count]);

  // Функция для отрисовки DVD логотипа (как в примере)
  const drawDVDLogo = (
    context: CanvasRenderingContext2D,
    logo: DVDLogo,
    svg: Element
  ) => {
    const scale = 2;

    context.save();
    context.translate(logo.x, logo.y);
    context.scale(scale, scale);

    // Парсим SVG и рисуем по частям (как в примере)
    const descendants = svg.querySelectorAll(":scope *");

    for (const descendant of descendants) {
      const element = descendant as SVGElement;

      if (element.nodeName === "path") {
        const path = new Path2D(element.getAttribute("d") || "");
        const fill = element.getAttribute("fill");
        context.fillStyle = fill === "#fff" ? "#000000" : logo.color;
        context.fill(path);
      } else if (element.nodeName === "ellipse") {
        context.beginPath();
        const cx = Number(element.getAttribute("cx") || "0");
        const cy = Number(element.getAttribute("cy") || "0");
        const rx = Number(element.getAttribute("rx") || "0");
        const ry = Number(element.getAttribute("ry") || "0");
        context.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
        context.closePath();
        const fill = element.getAttribute("fill");
        context.fillStyle = fill === "#fff" ? "#000000" : logo.color;
        context.fill();
      }
    }

    context.restore();
  };

  return (
    <canvas
      ref={canvasReference}
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        width: "100vw",
        height: "100vh",
        pointerEvents: "none",
        zIndex: 1000,
      }}
    />
  );
}
