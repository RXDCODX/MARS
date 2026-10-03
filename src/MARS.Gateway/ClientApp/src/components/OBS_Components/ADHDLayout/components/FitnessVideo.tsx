import { getVideoPath } from "./imageAssets";

export function FitnessVideo() {
  return (
    <video
      autoPlay
      loop
      muted
      playsInline
      style={{
        objectFit: "cover",
        position: "absolute",
        bottom: "0",
        right: "21vw",
        zIndex: 1000,
        transform: "scale(0.55)",
        transformOrigin: "bottom",
      }}
    >
      <source src={getVideoPath("fitness")} type="video/webm" />
      Ваш браузер не поддерживает видео.
    </video>
  );
}
