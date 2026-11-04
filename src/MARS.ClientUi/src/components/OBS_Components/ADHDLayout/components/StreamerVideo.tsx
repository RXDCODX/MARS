import { getVideoPath } from "./imageAssets";

export function StreamerVideo() {
  return (
    <video
      autoPlay
      loop
      muted
      playsInline
      style={{
        objectFit: "cover",
        position: "absolute",
        bottom: "0px",
        left: "0px",
        zIndex: 1000,
        transform: "scale(0.7)",
        transformOrigin: "bottom left",
      }}
    >
      <source src={getVideoPath("streamer")} type="video/webm" />
      Ваш браузер не поддерживает видео.
    </video>
  );
}
