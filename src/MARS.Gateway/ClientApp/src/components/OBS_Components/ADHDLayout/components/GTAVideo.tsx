import { getVideoPath } from "./imageAssets";

export function GTAVideo() {
  return (
    <video
      autoPlay
      loop
      muted
      playsInline
      style={{
        objectFit: "fill",
        position: "absolute",
        bottom: "29vh",
        left: "0",
        width: "300px",
        zIndex: 1000,
        transformOrigin: "left",
      }}
    >
      <source src={getVideoPath("gta")} type="video/mp4" />
      Ваш браузер не поддерживает видео.
    </video>
  );
}
