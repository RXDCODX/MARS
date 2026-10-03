import { useEffect, useRef } from "react";

interface RainDrop {
  x: number;
  y: number;
  speed: number;
  length: number;
  opacity: number;
  thickness: number;
}

export const RainEffect = (): React.ReactElement => {
  const canvasReference = useRef<HTMLCanvasElement>(null);
  const animationReference = useRef<number>(0);

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;

    const context = canvas.getContext("2d");
    if (!context) return;

    // Устанавливаем размер canvas на весь экран
    const resizeCanvas = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };

    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);

    // Создаем капли дождя
    const raindrops: RainDrop[] = [];
    const maxDrops = 400; // Увеличил с 200 до 400

    for (let index = 0; index < maxDrops; index++) {
      raindrops.push({
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height - canvas.height,
        speed: 4 + Math.random() * 6, // Увеличил скорость с 2-5 до 4-10
        length: 20 + Math.random() * 40, // Увеличил длину с 10-30 до 20-60
        opacity: 0.3 + Math.random() * 0.7, // Увеличил прозрачность с 0.1-0.4 до 0.3-1.0
        thickness: 1 + Math.random() * 2, // Добавил толщину капель
      });
    }

    // Анимация дождя
    const animate = () => {
      context.clearRect(0, 0, canvas.width, canvas.height);

      // Более яркий и заметный цвет дождя
      context.strokeStyle = "#4fc3f7"; // Яркий голубой цвет
      context.lineCap = "round";

      for (const drop of raindrops) {
        context.globalAlpha = drop.opacity;
        context.lineWidth = drop.thickness; // Используем толщину для каждой капли

        context.beginPath();
        context.moveTo(drop.x, drop.y);
        context.lineTo(drop.x, drop.y + drop.length);
        context.stroke();

        // Обновляем позицию капли
        drop.y += drop.speed;

        // Если капля вышла за пределы экрана, возвращаем её наверх
        if (drop.y > canvas.height) {
          drop.y = -drop.length;
          drop.x = Math.random() * canvas.width;
        }
      }

      context.globalAlpha = 1;
      animationReference.current = requestAnimationFrame(animate);
    };

    animate();

    return () => {
      if (animationReference.current) {
        cancelAnimationFrame(animationReference.current);
      }
      window.removeEventListener("resize", resizeCanvas);
    };
  }, []);

  return (
    <canvas
      ref={canvasReference}
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        width: "100%",
        height: "100%",
        pointerEvents: "none",
        zIndex: 1000,
      }}
    />
  );
};
