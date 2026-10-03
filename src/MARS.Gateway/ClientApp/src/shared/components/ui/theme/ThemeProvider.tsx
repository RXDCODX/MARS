import { ConfigProvider } from "antd";
import type { ReactNode } from "react";

import useAntdTheme from "./useAntdTheme";

interface ThemeProviderProperties {
  children: ReactNode;
}

const AntdThemeProvider = ({ children }: ThemeProviderProperties) => {
  const configProperties = useAntdTheme();

  return <ConfigProvider {...configProperties}>{children}</ConfigProvider>;
};

export default AntdThemeProvider;
