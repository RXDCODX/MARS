import { fileURLToPath } from "node:url";

import type { StorybookConfig } from "@storybook/react-vite";
import path from "path";

const dirname = path.dirname(fileURLToPath(import.meta.url));

const config: StorybookConfig = {
  stories: ["../src/**/*.mdx", "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  typescript: {
    reactDocgen: false,
  },
  addons: [
    "@chromatic-com/storybook",
    "@storybook/addon-docs",
    "@storybook/addon-onboarding",
    "@storybook/addon-a11y",
    "@storybook/addon-vitest",
  ],
  framework: {
    name: "@storybook/react-vite",
    options: {},
  },
  staticDirs: [
    "../public",
    "../src/overlays/FumoFriday/FumosVideos",
    "../src/assets/faces",
  ],
  viteFinal: async config => {
    // Настройка для GitHub Pages
    if (config.base === undefined) {
      config.base = "./";
    }

    // Настройки для обработки видео файлов
    config.assetsInclude = config.assetsInclude || [];
    if (Array.isArray(config.assetsInclude)) {
      config.assetsInclude.push("**/*.webm", "**/*.mp4", "**/*.gif");
    }

    // Настройки для обработки ассетов
    config.define = {
      ...config.define,
      "import.meta.env.PROD": false,
      "import.meta.env.DEV": true,
    };

    // Алиасы берутся из tsconfig.app.json через vite-tsconfig-paths
    config.plugins = config.plugins || [];
    const { default: tsconfigPaths } = await import("vite-tsconfig-paths");
    config.plugins.push(tsconfigPaths({ projects: [path.resolve(dirname, "../tsconfig.app.json")] }));

    // Настройки для CSS модулей
    if (config.css) {
      config.css.modules = {
        ...config.css.modules,
        localsConvention: "camelCase",
        generateScopedName: "[name]__[local]___[hash:base64:5]",
      };
    }

    return config;
  },
  env: config => ({
    ...config,
    VITE_STATE: "book",
  }),
};
export default config;