import { getImagePath } from "./imageAssets";

export const LOFIGirl = () => (
  <picture
    style={{
      objectFit: "cover",
      position: "absolute",
      right: "30vh",
      bottom: "0",
      zIndex: 1000,
      transform: "scale(0.55)",
      transformOrigin: "bottom",
    }}
  >
    <source src={getImagePath("lofiGirl")} />
    <img src={getImagePath("lofiGirl")} alt="LOFI Girl" />
  </picture>
);
