import type { ConfigProviderProps } from "antd";
import { theme } from "antd";
import { createStyles } from "antd-style";
import { useMemo } from "react";

const useStyles = createStyles(({ css }) => ({
  buttonPrimary: css({
    backgroundColor: "#1976d2",
    color: "#ffffff",
    border: "none",
    fontWeight: 500,
    textTransform: "uppercase",
    letterSpacing: "0.02857em",
    boxShadow:
      "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)",
    transition: "all 0.2s cubic-bezier(0.4, 0, 0.2, 1)",
  }),
  buttonDefault: css({
    backgroundColor: "#ffffff",
    color: "rgba(0, 0, 0, 0.87)",
    border: "1px solid rgba(0, 0, 0, 0.23)",
    fontWeight: 500,
    textTransform: "uppercase",
    letterSpacing: "0.02857em",
    transition: "all 0.2s cubic-bezier(0.4, 0, 0.2, 1)",
  }),
  buttonDanger: css({
    backgroundColor: "#d32f2f",
    color: "#ffffff",
    border: "none",
    fontWeight: 500,
    textTransform: "uppercase",
    letterSpacing: "0.02857em",
    boxShadow:
      "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)",
  }),
  inputRoot: css({
    borderColor: "rgba(0, 0, 0, 0.23)",
    transition: "all 0.2s cubic-bezier(0.4, 0, 0.2, 1)",
  }),
  inputError: css({ borderColor: "#d32f2f" }),
  selectRoot: css({
    borderColor: "rgba(0, 0, 0, 0.23)",
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
  }),
  selectPopup: css({
    borderRadius: "4px",
    boxShadow:
      "0px 5px 5px -3px rgba(0,0,0,0.2), 0px 8px 10px 1px rgba(0,0,0,0.14), 0px 3px 14px 2px rgba(0,0,0,0.12)",
  }),
}));

export const useMuiTheme = (isDark: boolean) => {
  const { styles } = useStyles();

  return useMemo<ConfigProviderProps>(
    () => ({
      theme: {
        algorithm: isDark ? theme.darkAlgorithm : theme.defaultAlgorithm,
        token: {
          colorPrimary: "#1976d2",
          colorSuccess: "#2e7d32",
          colorWarning: "#ed6c02",
          colorError: "#d32f2f",
          colorInfo: "#0288d1",
          colorTextBase: isDark ? "#e0e0e0" : "#212121",
          colorBgBase: isDark ? "#121212" : "#fafafa",
          colorText: isDark
            ? "rgba(255, 255, 255, 0.87)"
            : "rgba(33, 33, 33, 0.87)",
          colorTextSecondary: isDark
            ? "rgba(255, 255, 255, 0.6)"
            : "rgba(33, 33, 33, 0.6)",
          colorBorder: isDark ? "#424242" : "#e0e0e0",
          colorBorderSecondary: isDark ? "#303030" : "#eeeeee",
          colorBgContainer: isDark ? "#1e1e1e" : "#ffffff",
          colorBgElevated: isDark ? "#2d2d2d" : "#ffffff",
          colorBgLayout: isDark ? "#121212" : "#f5f5f5",
          borderRadius: 4,
          borderRadiusXS: 1,
          borderRadiusSM: 2,
          borderRadiusLG: 6,
          boxShadow:
            "0px 2px 1px -1px rgba(0,0,0,0.2),0px 1px 1px 0px rgba(0,0,0,0.14),0px 1px 3px 0px rgba(0,0,0,0.12)",
          boxShadowSecondary:
            "0px 3px 3px -2px rgba(0,0,0,0.2),0px 3px 4px 0px rgba(0,0,0,0.14),0px 1px 8px 0px rgba(0,0,0,0.12)",
        },
        components: {
          Button: {
            primaryShadow:
              "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)",
            defaultShadow:
              "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)",
            dangerShadow:
              "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)",
            fontWeight: 500,
            borderRadius: 4,
          },
          Alert: { borderRadiusLG: 4 },
          Modal: { borderRadiusLG: 4 },
          Checkbox: { borderRadiusSM: 2 },
        },
      },
      button: {
        classNames: ({ props }) => ({
          root:
            props.type === "primary"
              ? styles.buttonPrimary
              : props.danger
                ? styles.buttonDanger
                : styles.buttonDefault,
        }),
      },
      input: {
        classNames: ({ props }) => ({
          root: props.status === "error" ? styles.inputError : styles.inputRoot,
        }),
      },
      select: {
        classNames: {
          root: styles.selectRoot,
          popup: { root: styles.selectPopup },
        },
      },
    }),
    [isDark, styles]
  );
};
