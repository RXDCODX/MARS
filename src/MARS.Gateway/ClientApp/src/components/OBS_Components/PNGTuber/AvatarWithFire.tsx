import EBALO4Url from "@/components/OBS_Components/PNGTuber/png/EBALO4.png";

import { FireOutlineCanvas } from "./FireShader";

// Демо-компонент: PNG-картинка + огненная обводка поверх. Параметры заметности
export const AvatarWithFire = () => {
  const size = 512;
  const radius = Math.floor(size * 0.14);
  const intensity = 1.6;
  const speed = 1.35;

  return (
    <div
      style={{
        width: size,
        height: size,
        position: "relative",
        background: "#000",
      }}
    >
      <img
        alt="avatar-mask"
        src={EBALO4Url}
        style={{
          width: "100%",
          height: "100%",
          objectFit: "contain",
          display: "block",
          userSelect: "none",
          pointerEvents: "none",
          position: "absolute",
          inset: 0,
          zIndex: 0,
        }}
      />
      <FireOutlineCanvas
        width={size}
        height={size}
        maskUrl={EBALO4Url}
        radiusPx={radius}
        intensity={intensity}
        speed={speed}
        style={{ position: "absolute", inset: 0 }}
      />
    </div>
  );
};
