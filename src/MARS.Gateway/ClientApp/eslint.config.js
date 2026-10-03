// For more info, see https://github.com/storybookjs/eslint-plugin-storybook#configuration-flat-config-format
//*@ts-check*
import { fixupConfigRules } from "@eslint/compat";
import js from "@eslint/js";
import typescriptEslint from "@typescript-eslint/eslint-plugin";
import typescriptParser from "@typescript-eslint/parser";
import eslintConfigPrettier from "eslint-config-prettier";
import prettier from "eslint-plugin-prettier"; // Import the Prettier plugin
import react from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import simpleImportSort from "eslint-plugin-simple-import-sort";
import storybook from "eslint-plugin-storybook";
import unicorn from "eslint-plugin-unicorn";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  js.configs.recommended,
  tseslint.configs.recommended,
  unicorn.configs["flat/recommended"],
  {
    rules: {
      "unicorn/logical-assignment-operators": "off",
    },
  },
  {
    plugins: {
      prettier: prettier,
    },
    rules: {
      "prettier/prettier": [
        "warn",
        {
          endOfLine: "auto",
        },
      ],
    },
  },
  ...fixupConfigRules({
    files: ["**/*.{js,jsx,ts,tsx}"],
    languageOptions: {
      ecmaVersion: "latest",
      globals: globals.browser,
    },
    settings: {
      react: {
        version: "detect",
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
      typescriptEslint,
      "simple-import-sort": simpleImportSort,
      react: react,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "unicode-bom": "error",
      "arrow-body-style": "error",
      "prefer-arrow-callback": "error",
      "react/jsx-uses-react": "off",
      "react/react-in-jsx-scope": "off",
      "simple-import-sort/imports": "error",
      "react/no-children-prop": "error",
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "react",
              importNames: ["default"],
              message: "Импортируйте конкретные объекты, а не весь React.",
            },
          ],
        },
      ],
      "react-refresh/only-export-components": [
        "error",
        { allowConstantExport: true },
      ],
    },
    ignores: [
      "src/services/*",
      "dist/**",
      "node_modules/**",
      "**/*.md",
      "build/**",
      "vite.config.ts",
      "obj/**",
    ],
  }),
  {
    files: ["**/*.{js,ts,tsx}"],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        ecmaFeatures: {
          jsx: true,
        },
        ecmaVersion: 11,
        sourceType: "module",
      },
    },
  },
  eslintConfigPrettier,
  storybook.configs["flat/recommended"]
);
