import type { ConfigProviderProps } from "antd";
import type { MappingAlgorithm } from "antd/es/theme/interface";
import { theme } from "antd";
import { createStyles } from "antd-style";
import { useMemo } from "react";

import { Theme, useTheme } from "@/contexts/Theme";

import { AntdStyle, useAntdStyle } from "./AntdStyleContext";

const useStyles = createStyles(({ css, cssVar }) => {
  const illustrationBorder = {
    border: `${cssVar.lineWidth} solid ${cssVar.colorBorder}`,
  };

  const illustrationBox = {
    ...illustrationBorder,
    boxShadow: `4px 4px 0 ${cssVar.colorBorder}`,
  };

  return {
    illustrationBorder,
    illustrationBox,
    buttonRoot: css({
      ...illustrationBox,
      fontWeight: 600,
      textTransform: "uppercase",
      letterSpacing: "0.5px",
    }),
    modalContainer: css({
      ...illustrationBox,
    }),
    tooltipRoot: css({
      padding: cssVar.padding,
    }),
    popupBox: css({
      ...illustrationBox,
      borderRadius: cssVar.borderRadiusLG,
      backgroundColor: cssVar.colorBgContainer,
    }),
    progressRail: css({
      border: `${cssVar.lineWidth} solid ${cssVar.colorBorder}`,
      boxShadow: `2px 2px 0 ${cssVar.colorBorder}`,
    }),
    progressTrack: css({
      border: "none",
    }),
    inputNumberActions: css({
      width: 12,
    }),
  };
});

const lightTokens = {
  colorText: "#2C2C2C",
  colorPrimary: "#52C41A",
  colorSuccess: "#51CF66",
  colorWarning: "#FFD93D",
  colorError: "#FA5252",
  colorInfo: "#4DABF7",
  colorBorder: "#2C2C2C",
  colorBorderSecondary: "#dee2e6",
  colorBgBase: "#FFF9F0",
  colorBgContainer: "#FFFFFF",
  colorBgElevated: "#FFFFFF",
  colorBgLayout: "#F5F5F5",
};

const darkTokens = {
  colorText: "#E8E8E8",
  colorPrimary: "#73d13d",
  colorSuccess: "#73d13d",
  colorWarning: "#FFD93D",
  colorError: "#FF6B6B",
  colorInfo: "#69B1FF",
  colorBorder: "#424242",
  colorBorderSecondary: "#303030",
  colorBgBase: "#141414",
  colorBgContainer: "#1F1F1F",
  colorBgElevated: "#262626",
  colorBgLayout: "#0A0A0A",
};

/**
 * Стили antd, которые дополнительно используют компактный алгоритм.
 *
 * Список пуст не потому, что компактность забыта, а потому что ни один из
 * доступных стилей её не подразумевает: `AntdStyle` — это имена оформления
 * («illustration», «mui», «cartoon», «geek»), а не имена алгоритмов antd.
 *
 * Раньше здесь стояло `antdStyle === "compact" || antdStyle === "dark-compact"`.
 * Таких значений в типе нет, и обе проверки были ложны навсегда: тип сравнивался
 * с именами алгоритмов, каких типу не выдавали. Компактность отсюда не
 * включалась никогда — код выглядел работающим, а не был. Список сделан явным,
 * чтобы значение добавлялось одной строкой и чтобы его отсутствие было видно.
 */
const COMPACT_STYLES: readonly AntdStyle[] = [];

/**
 * Тьма берётся из темы сайта, а не из имени стиля antd.
 *
 * Имя стиля темноту не кодирует, а тема сайта кодирует — и именно по ней
 * выбираются токены строк ниже. Раньше проверки имени стиля были ложны, так что
 * фактически решала тема сайта; теперь это написано прямо.
 */
const isDarkTheme = (siteTheme: Theme): boolean => siteTheme === "dark";

const isCompactStyle = (antdStyle: AntdStyle): boolean =>
  COMPACT_STYLES.includes(antdStyle);

/**
 * Алгоритмы antd для этой темы.
 *
 * `ConfigProvider` принимает либо один алгоритм, либо их список. Раньше здесь
 * стояло `algorithms.length === 1 ? algorithms[0] : algorithms`, и TypeScript
 * отказывался принять результат: тип `algorithm` — это объединение
 * «алгоритм либо список», и элемент списка сам по себе таким объединением не
 * является. Возвращается всегда список — ConfigProvider обрабатывает список из
 * одного элемента так же, как один алгоритм, — и тип становится точным.
 */
const getAlgorithms = (
  antdStyle: AntdStyle,
  siteTheme: Theme
): MappingAlgorithm[] => {
  const algorithms: MappingAlgorithm[] = [
    isDarkTheme(siteTheme) ? theme.darkAlgorithm : theme.defaultAlgorithm,
  ];

  if (isCompactStyle(antdStyle)) {
    algorithms.push(theme.compactAlgorithm);
  }

  return algorithms;
};

const useIllustrationTheme = () => {
  const { styles } = useStyles();
  const { theme: siteTheme } = useTheme();
  const { antdStyle } = useAntdStyle();

  const isDark = isDarkTheme(siteTheme);
  const algorithms = getAlgorithms(antdStyle, siteTheme);

  return useMemo<ConfigProviderProps>(
    () => ({
      theme: {
        algorithm: algorithms,
        token: {
          ...(isDark ? darkTokens : lightTokens),
          lineWidth: 3,
          lineWidthBold: 3,
          borderRadius: 12,
          borderRadiusLG: 16,
          borderRadiusSM: 8,
          controlHeight: 40,
          controlHeightSM: 34,
          controlHeightLG: 48,
          fontSize: 15,
          fontWeightStrong: 600,
        },
        components: {
          Button: {
            primaryShadow: "none",
            dangerShadow: "none",
            defaultShadow: "none",
            fontWeight: 600,
          },
          Modal: {
            boxShadow: "none",
          },
          Card: {
            boxShadow: isDark ? "4px 4px 0 #424242" : "4px 4px 0 #2C2C2C",
            colorBgContainer: isDark ? "#1F1F1F" : "#FFF0F6",
          },
          Tooltip: {
            colorBorder: isDark ? "#424242" : "#2C2C2C",
            colorBgSpotlight: isDark
              ? "rgba(50, 50, 50, 0.95)"
              : "rgba(100, 100, 100, 0.95)",
            borderRadius: 8,
          },
          Select: {
            optionSelectedBg: "transparent",
          },
          Slider: {
            dotBorderColor: "#237804",
            dotActiveBorderColor: "#237804",
            colorPrimaryBorder: "#237804",
            colorPrimaryBorderHover: "#237804",
          },
        },
      },
      button: {
        classNames: {
          root: styles.buttonRoot,
        },
      },
      modal: {
        classNames: {
          container: styles.modalContainer,
        },
      },
      alert: {
        className: styles.illustrationBorder,
      },
      colorPicker: {
        arrow: false,
        classNames: {
          root: styles.illustrationBox,
        },
      },
      popover: {
        classNames: {
          container: styles.illustrationBox,
        },
      },
      tooltip: {
        arrow: false,
        classNames: {
          root: styles.tooltipRoot,
          container: styles.illustrationBox,
        },
      },
      dropdown: {
        classNames: {
          root: styles.popupBox,
        },
      },
      select: {
        classNames: {
          root: styles.illustrationBox,
          popup: {
            root: styles.popupBox,
          },
        },
      },
      input: {
        classNames: {
          root: styles.illustrationBox,
        },
      },
      inputNumber: {
        classNames: {
          root: styles.illustrationBox,
          actions: styles.inputNumberActions,
        },
      },
      progress: {
        classNames: {
          rail: styles.progressRail,
          track: styles.progressTrack,
        },
        styles: {
          rail: {
            height: 16,
          },
          track: {
            height: 10,
          },
        },
      },
    }),
    [algorithms, isDark, styles]
  );
};

export default useIllustrationTheme;
