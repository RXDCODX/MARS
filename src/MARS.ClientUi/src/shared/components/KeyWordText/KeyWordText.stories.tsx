import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";

import { KeyWordText } from "./index";

const meta: Meta<typeof KeyWordText> = {
  title: "Shared/KeyWordText",
  component: KeyWordText,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Компонент для выделения ключевых слов в тексте.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    keyWordedString: {
      control: "text",
    },
    keySymbol: {
      control: "text",
    },
    keyWordColor: {
      control: "color",
    },
    classNameForKeyWordedSpan: {
      control: "text",
    },
    isQuouted: {
      control: "boolean",
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    keyWordedString: "Hello #world! This is a #test message.",
    keySymbol: "#",
    keyWordColor: "#ff0000",
    classNameForKeyWordedSpan: "keyword",
    isQuouted: false,
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие текста
    const textElement = canvasElement.querySelector('[class*="keyword"]');
    expect(textElement).toBeInTheDocument();

    // Проверяем, что текст отображается
    expect(canvasElement).toHaveTextContent(
      "Hello #world! This is a #test message."
    );

    // Проверяем выделение ключевых слов
    const keywords = canvasElement.querySelectorAll('[class*="keyword"]');
    expect(keywords.length).toBeGreaterThan(0);

    // Проверяем цвет ключевых слов
    const firstKeyword = keywords[0];
    const computedStyle = globalThis.getComputedStyle(firstKeyword);
    expect(computedStyle.color).toBe("rgb(255, 0, 0)");
  },
};

export const WithQuotes: Story = {
  args: {
    keyWordedString: 'This is a "quoted #keyword" example.',
    keySymbol: "#",
    keyWordColor: "#00ff00",
    classNameForKeyWordedSpan: "highlight",
    isQuouted: true,
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие текста с кавычками
    const textElement = canvasElement.querySelector('[class*="highlight"]');
    expect(textElement).toBeInTheDocument();

    // Проверяем, что текст отображается
    expect(canvasElement).toHaveTextContent(
      'This is a "quoted #keyword" example.'
    );

    // Проверяем выделение ключевых слов в кавычках
    const keywords = canvasElement.querySelectorAll('[class*="highlight"]');
    expect(keywords.length).toBeGreaterThan(0);

    // Проверяем цвет ключевых слов
    const firstKeyword = keywords[0];
    const computedStyle = globalThis.getComputedStyle(firstKeyword);
    expect(computedStyle.color).toBe("rgb(0, 255, 0)");
  },
};

export const MultipleKeywords: Story = {
  args: {
    keyWordedString:
      "Multiple #keywords in #one #sentence with #different #symbols.",
    keySymbol: "#",
    keyWordColor: "#0000ff",
    classNameForKeyWordedSpan: "multiple-keywords",
    isQuouted: false,
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем наличие множественных ключевых слов
    const keywords = canvasElement.querySelectorAll(
      '[class*="multiple-keywords"]'
    );
    expect(keywords.length).toBeGreaterThan(1);

    // Проверяем, что все ключевые слова выделены
    keywords.forEach(keyword => {
      const computedStyle = globalThis.getComputedStyle(keyword);
      expect(computedStyle.color).toBe("rgb(0, 0, 255)");
    });

    // Проверяем, что текст отображается полностью
    expect(canvasElement).toHaveTextContent(
      "Multiple #keywords in #one #sentence"
    );
  },
};

export const NoKeywords: Story = {
  args: {
    keyWordedString: "This text has no keywords to highlight.",
    keySymbol: "#",
    keyWordColor: "#ff00ff",
    classNameForKeyWordedSpan: "no-keywords",
    isQuouted: false,
  },
  play: async ({ canvasElement }) => {
    // Ждем появления компонента
    await new Promise(resolve => setTimeout(resolve, 100));

    // Проверяем, что компонент отрендерился
    expect(canvasElement).toBeInTheDocument();

    // Проверяем, что нет выделенных ключевых слов
    const keywords = canvasElement.querySelectorAll('[class*="no-keywords"]');
    expect(keywords.length).toBe(0);

    // Проверяем, что текст отображается
    expect(canvasElement).toHaveTextContent(
      "This text has no keywords to highlight."
    );
  },
};
