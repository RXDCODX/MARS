import { getVideoPath } from "./imageAssets";

export function SlimeVideo() {
  return (
    <video
      autoPlay
      loop
      muted
      playsInline
      style={{
        objectFit: "fill",
        position: "absolute",
        top: "6.2vh",
        left: "0px",
        zIndex: 1000,
        width: "250px",
        transformOrigin: "left",
      }}
    >
      <source src={getVideoPath("slime")} type="video/webm" />
      Ваш браузер не поддерживает видео.
    </video>
  );
}
