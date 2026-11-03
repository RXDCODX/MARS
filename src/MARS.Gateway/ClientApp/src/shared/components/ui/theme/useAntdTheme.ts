import type { ConfigProviderProps } from "antd";
import { useMemo } from "react";

import { useTheme } from "@/contexts/Theme";
import { useAntdStyle } from "./AntdStyleContext";
import { useCartoonTheme } from "./themes/cartoonTheme";
import { useGeekTheme } from "./themes/geekTheme";
import { useIllustrationTheme } from "./themes/illustrationTheme";
import { MODERN_ANTD_STYLES, useModernTheme } from "./themes/modernThemes";
import { useMuiTheme } from "./themes/muiTheme";

const useAntdTheme = () => {
  const { theme: siteTheme } = useTheme();
  const { antdStyle } = useAntdStyle();

  const isDark = antdStyle === "geek" || siteTheme === "dark";

  const illustrationProperties = useIllustrationTheme(isDark);
  const muiProperties = useMuiTheme(isDark);
  const cartoonProperties = useCartoonTheme(isDark);
  const geekProperties = useGeekTheme(isDark);
  const modernProperties = useModernTheme(antdStyle, isDark);

  return useMemo<ConfigProviderProps>(() => {
    const isModernStyle = MODERN_ANTD_STYLES.includes(
      antdStyle as (typeof MODERN_ANTD_STYLES)[number]
    );

    if (isModernStyle) {
      return modernProperties;
    }

    switch (antdStyle) {
      case "mui": {
        return muiProperties;
      }
      case "cartoon": {
        return cartoonProperties;
      }
      case "geek": {
        return geekProperties;
      }
      case "illustration":
      default: {
        return illustrationProperties;
      }
    }
  }, [
    antdStyle,
    illustrationProperties,
    muiProperties,
    cartoonProperties,
    geekProperties,
    modernProperties,
  ]);
};

export default useAntdTheme;
