import type { ConfigProviderProps } from "antd";
import { useMemo } from "react";

import { Theme, useTheme } from "@/contexts/Theme";

import { AntdStyle, useAntdStyle } from "./AntdStyleContext";
import { useCartoonTheme } from "./themes/cartoonTheme";
import { useGeekTheme } from "./themes/geekTheme";
import { useIllustrationTheme } from "./themes/illustrationTheme";
import { useMuiTheme } from "./themes/muiTheme";

const useAntdTheme = () => {
  const { theme: siteTheme } = useTheme();
  const { antdStyle } = useAntdStyle();

  const isDark = antdStyle === "geek" || siteTheme === "dark";

  const illustrationProperties = useIllustrationTheme(isDark);
  const muiProperties = useMuiTheme(isDark);
  const cartoonProperties = useCartoonTheme(isDark);
  const geekProperties = useGeekTheme(isDark);

  return useMemo<ConfigProviderProps>(() => {
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
  ]);
};

export default useAntdTheme;
