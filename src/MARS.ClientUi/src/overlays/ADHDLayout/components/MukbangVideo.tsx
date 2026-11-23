import { getVideoPath } from "./imageAssets";

export function MukbangVideo() {
  return (
    <video
      autoPlay
      loop
      muted
      playsInline
      style={{
        objectFit: "fill",
        position: "absolute",
        top: "31vh",
        left: "0",
        width: "300px",
        zIndex: 1000,
        transformOrigin: "left",
      }}
    >
      <source src={getVideoPath("mukbang")} type="video/webm" />
      Ваш браузер не поддерживает видео.
    </video>
  );
}
