import { CSSProperties } from "react";

import { MediaDto } from "@/shared/api";

export function getMediaFrameStyle(mediaInfo: MediaDto): CSSProperties {
  if (!mediaInfo.mediaInfo.stylesInfo.isBorder) {
    return {};
  }

  return {
    boxShadow: "0 0 0 12px #ffffff",
    borderRadius: "12px",
    backgroundColor: "#ffffff",
  };
}
