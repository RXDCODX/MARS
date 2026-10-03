// eslint-disable-next-line no-restricted-imports
import React, { useEffect } from "react";

import { ThemeProvider } from "../src/contexts/ThemeContext";

export const withStorybookAttribute = (Story) => {
  const Wrapper = (props) => {
    useEffect(() => {
      document.documentElement.setAttribute("data-storybook", "true");
      return () => {
        document.documentElement.removeAttribute("data-storybook");
      };
    }, []);
    return <Story {...props} />;
  };
  return <Wrapper />;
};

export const withThemeProvider = (Story) => {
  return (
    <ThemeProvider>
      <Story />
    </ThemeProvider>
  );
};
