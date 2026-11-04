import "./Announce.module.scss";

import { CSSProperties, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";

import { useInjectStyles } from "@/shared/hooks";

interface Properties {
  title: string;
  callback?: () => void;
}

const getRandomColor = (): string => {
  const letters = "0123456789ABCDEF";
  let color = "#";
  for (let index = 0; index < 6; index++) {
    color += letters[Math.floor(Math.random() * 16)];
  }
  return color;
};

export default function Announce({ title, callback }: Properties) {
  const nodeReference = useRef(null);
  // Цвет фиксируется на маунте
  const [backgroundColor] = useState(getRandomColor);

  useInjectStyles(`
    #announce{
      text-shadow:
        rgb(0 0 0) 2px 0 0,
        rgb(0 0 0) 1.75517px 0.958851px 0,
        rgb(0 0 0) 1.0806px 1.68294px 0,
        rgb(0 0 0) 0.141474px 1.99499px 0,
        rgb(0 0 0) -0.832294px 1.81859px 0,
        rgb(0 0 0) -1.60229px 1.19694px 0,
        rgb(0 0 0) -1.97998px 0.28224px 0,
        rgb(0 0 0) -1.87291px -0.701566px 0,
        rgb(0 0 0) -1.30729px -1.5136px 0,
        rgb(0 0 0) -0.421592px -1.95506px 0,
        rgb(0 0 0) 0.567324px -1.91785px 0,
        rgb(0 0 0) 1.41734px -1.41108px 0,
        rgb(0 0 0) 1.92034px -0.558831px 0;

      :global .animated-gradient-text {
        text-shadow: none !important;
      }

      & .animated-gradient-text {
        text-shadow: none !important;
      }
    }
    `);

  // Отключить в Storybook
  if (import.meta.env.VITE_STATE === "book") {
    return null;
  }

  document.title = title;

  const defaultStyle: CSSProperties = {
    position: "absolute" /* Абсолютное позиционирование */,
    top: "50%" /* Центрирование по вертикали */,
    left: "50%" /* Центрирование по горизонтали */,
    transform:
      "translate(-50%, -50%)" /* Сдвиг на половину ширины и высоты элемента для центрирования */,
    display: "flex",
    flexDirection: "column" /* Размещение элементов в колонку */,
    justifyContent: "center" /* Центрирование содержимого по вертикали */,
    alignItems: "center" /* Центрирование содержимого по горизонтали */,
    height: "30%" /* Высота блока будет зависеть от содержимого */,
    width: "80%" /* Ширина блока будет 80% от ширины экрана */,
    padding: "20px" /* Добавление отступов внутри блока */,
    backgroundColor: backgroundColor,
  };

  interface FadeOutProperties {
    duration: number;
    delay: number;
    children: React.ReactNode;
    callback: () => void;
  }

  const FadeOut: React.FC<FadeOutProperties> = ({
    duration,
    delay,
    children,
    callback,
  }) => {
    const elementReference = useRef<HTMLDivElement>(null);
    const [isVisible, setIsVisible] = useState(true);

    useEffect(() => {
      const element = elementReference.current;
      if (element) {
        const delayTimer = setTimeout(() => {
          if (!element) {
            return;
          }

          element.style.transition = `opacity ${duration}ms`;
          element.style.opacity = "0";

          const fadeOutTimer = setTimeout(() => {
            setIsVisible(false);
            callback();
          }, duration);

          return () => {
            clearTimeout(fadeOutTimer);
          };
        }, delay);

        return () => {
          clearTimeout(delayTimer);
        };
      }
    }, [duration, delay, callback]);

    return isVisible ? <div ref={elementReference}>{children}</div> : null;
  };

  return (
    <>
      <FadeOut
        duration={2000}
        delay={3000}
        callback={() => {
          if (callback) {
            callback();
          }
        }}
      >
        <div ref={nodeReference} style={defaultStyle} className="announce">
          <Textfit
            id="announce"
            forceSingleModeWidth
            min={1}
            max={1500}
            style={{
              width: "100%",
              height: "100%",
              alignItems: "center",
              display: "flex",
              justifySelf: "center",
              alignSelf: "center",
              justifyContent: "center",
            }}
          >
            {title}
          </Textfit>
        </div>
      </FadeOut>
    </>
  );
}
