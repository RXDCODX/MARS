import type { ConfigProviderProps } from "antd";
import { theme } from "antd";
import { createStyles } from "antd-style";
import { useMemo } from "react";

const useStyles = createStyles(({ css, cssVar }) => {
  const lightBorder = {
    border: `${cssVar.lineWidth} solid ${cssVar.colorPrimary}`,
    boxShadow: `0 0 3px ${cssVar.colorPrimary}, inset 0 0 10px ${cssVar.colorPrimary}`,
  };

  return {
    lightBorder,
    app: css({
      textShadow: `0 0 5px color-mix(in srgb, currentColor 50%, transparent)`,
    }),
    modalContainer: css({ ...lightBorder, padding: 0 }),
    modalHeader: css({
      padding: `${cssVar.padding} ${cssVar.paddingLG}`,
      margin: 0,
      position: "relative",
      "&:after": {
        ...lightBorder,
        content: '""',
        position: "absolute",
        insetInline: 0,
        bottom: 0,
        border: 0,
        height: cssVar.lineWidth,
        background: cssVar.colorPrimary,
      },
    }),
    modalBody: css({ padding: `${cssVar.padding} ${cssVar.paddingLG}` }),
    modalFooter: css({ padding: `${cssVar.padding} ${cssVar.paddingLG}` }),
    buttonRoot: css({
      ...lightBorder,
      borderWidth: cssVar.lineWidth,
      borderColor: cssVar.colorPrimary,
    }),
    buttonRootSolid: css({
      color: cssVar.colorBgContainer,
      border: "none",
      fontWeight: "bolder",
    }),
    buttonRootSolidDanger: css({ boxShadow: `0 0 5px ${cssVar.colorError}` }),
    tooltipRoot: css({ padding: cssVar.padding }),
    tooltipContainer: css({ ...lightBorder, color: cssVar.colorPrimary }),
    progressTrack: css({ backgroundColor: cssVar.colorPrimary }),
  };
});

export const useGeekTheme = (_isDark: boolean) => {
  const { styles } = useStyles();

  return useMemo<ConfigProviderProps>(
    () => ({
      theme: {
        algorithm: theme.darkAlgorithm,
        token: {
          borderRadius: 0,
          lineWidth: 2,
          colorPrimary: "#39ff14",
          colorText: "#39ff14",
          colorInfo: "#39ff14",
          colorSuccess: "#39ff14",
          colorWarning: "#FFD93D",
          colorError: "#FF4444",
          colorBorder: "#39ff14",
          colorBorderSecondary: "#1a3a1a",
          colorBgBase: "#0a0a0a",
          colorBgContainer: "#0d0d0d",
          colorBgElevated: "#111111",
          colorBgLayout: "#000000",
          controlHeightSM: 26,
          controlHeight: 34,
        },
        components: {
          Button: {
            primaryShadow: "none",
            dangerShadow: "none",
            defaultShadow: "none",
          },
          Modal: { boxShadow: "none" },
        },
      },
      app: { className: styles.app },
      modal: {
        classNames: {
          container: styles.modalContainer,
          header: styles.modalHeader,
          body: styles.modalBody,
          footer: styles.modalFooter,
        },
      },
      button: {
        classNames: ({ props }) => ({
          root: props.danger
            ? styles.buttonRootSolidDanger
            : props.type === "primary"
              ? styles.buttonRootSolid
              : styles.buttonRoot,
        }),
      },
      alert: { className: styles.lightBorder },
      colorPicker: { arrow: false, classNames: { root: styles.lightBorder } },
      select: { classNames: { root: styles.lightBorder } },
      input: { classNames: { root: styles.lightBorder } },
      inputNumber: { classNames: { root: styles.lightBorder } },
      tooltip: {
        arrow: false,
        classNames: {
          root: styles.tooltipRoot,
          container: styles.tooltipContainer,
        },
      },
      progress: { classNames: { track: styles.progressTrack } },
    }),
    [styles]
  );
};
