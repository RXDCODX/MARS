import type { ConfigProviderProps } from "antd";
import { theme } from "antd";
import { createStyles } from "antd-style";
import { useMemo } from "react";

const useStyles = createStyles(({ css, cssVar }) => {
  const sharedBorder = {
    border: `${cssVar.lineWidth} ${cssVar.lineType} ${cssVar.colorBorder}`,
  };

  return {
    sharedBorder,
    progressTrack: css({
      ...sharedBorder,
      marginInlineStart: `calc(-1 * ${cssVar.lineWidth})`,
      marginBlockStart: `calc(-1 * ${cssVar.lineWidth})`,
    }),
  };
});

export const useCartoonTheme = (isDark: boolean) => {
  const { styles } = useStyles();

  return useMemo<ConfigProviderProps>(
    () => ({
      theme: {
        algorithm: isDark ? theme.darkAlgorithm : theme.defaultAlgorithm,
        token: {
          colorText: isDark ? "#E8E0D0" : "#51463B",
          colorPrimary: isDark ? "#66AAAA" : "#225555",
          colorError: isDark ? "#E8A0A0" : "#DA8787",
          colorInfo: isDark ? "#B0E0E0" : "#9CD3D3",
          colorInfoBorder: isDark ? "#66AAAA" : "#225555",
          colorBorder: isDark ? "#66AAAA" : "#225555",
          colorBorderSecondary: isDark ? "#555555" : "#225555",
          colorBgBase: isDark ? "#1A1A1A" : "#FAFAEE",
          colorBgContainer: isDark ? "#2A2A2A" : "#F5F0E8",
          colorBgElevated: isDark ? "#333333" : "#FAFAEE",
          colorBgLayout: isDark ? "#111111" : "#F0EBE0",
          lineWidth: 2,
          lineWidthBold: 2,
          borderRadius: 18,
          borderRadiusLG: 18,
          borderRadiusSM: 18,
          controlHeightSM: 28,
          controlHeight: 36,
        },
        components: {
          Button: {
            primaryShadow: "none",
            dangerShadow: "none",
            defaultShadow: "none",
          },
          Modal: { boxShadow: "none" },
          Card: { colorBgContainer: isDark ? "#333333" : "#BBAA99" },
          Tooltip: {
            borderRadius: 6,
            colorBorder: isDark ? "#66AAAA" : "#225555",
            algorithm: true,
          },
          Select: { optionSelectedBg: isDark ? "#444444" : "#CBC4AF" },
        },
      },
      modal: { classNames: { container: styles.sharedBorder } },
      colorPicker: { arrow: false },
      popover: { classNames: { container: styles.sharedBorder } },
      progress: {
        classNames: { rail: styles.sharedBorder, track: styles.progressTrack },
        styles: { rail: { height: 16 }, track: { height: 16 } },
      },
    }),
    [isDark, styles]
  );
};
