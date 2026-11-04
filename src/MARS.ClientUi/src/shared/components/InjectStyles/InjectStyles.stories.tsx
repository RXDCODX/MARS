import type { Meta, StoryObj } from "@storybook/react";
import { useState } from "react";

import { InjectStyles } from "./InjectStyles";

const meta: Meta<typeof InjectStyles> = {
  title: "Shared/InjectStyles",
  component: InjectStyles,
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof InjectStyles>;

/**
 * Базовый пример использования компонента InjectStyles
 */
export const Basic: Story = {
  render: () => (
    <>
      <InjectStyles
        styles={`
          .injected-styles-demo {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            font-size: 16px;
            text-align: center;
          }
        `}
        id="basic-demo-styles"
      />
      <div className="injected-styles-demo">
        Этот блок использует стили, внедренные через InjectStyles
      </div>
    </>
  ),
};

/**
 * Пример с динамическими стилями на основе состояния
 */
export const DynamicStyles: Story = {
  render: () => {
    const DynamicExample = () => {
      const [color, setColor] = useState("#ff0000");
      const [size, setSize] = useState(16);

      const dynamicStyles = `
        .dynamic-demo {
          background-color: ${color};
          font-size: ${size}px;
          color: white;
          padding: 20px;
          border-radius: 8px;
          transition: all 0.3s ease;
        }
      `;

      return (
        <>
          <InjectStyles styles={dynamicStyles} id="dynamic-demo-styles" />

          <div style={{ marginBottom: "20px" }}>
            <label style={{ marginRight: "10px" }}>
              Цвет:
              <input
                type="color"
                value={color}
                onChange={e => setColor(e.target.value)}
                style={{ marginLeft: "5px" }}
              />
            </label>

            <label>
              Размер шрифта: {size}px
              <input
                type="range"
                min="12"
                max="32"
                value={size}
                onChange={e => setSize(Number(e.target.value))}
                style={{ marginLeft: "5px" }}
              />
            </label>
          </div>

          <div className="dynamic-demo">
            Динамические стили, изменяющиеся в реальном времени
          </div>
        </>
      );
    };

    return <DynamicExample />;
  },
};

/**
 * Пример с несколькими наборами стилей
 */
export const MultipleStyleSets: Story = {
  render: () => (
    <>
      <InjectStyles
        styles={`
          .card-primary {
            background: #3b82f6;
            color: white;
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
          }
        `}
        id="card-primary-styles"
      />

      <InjectStyles
        styles={`
          .card-secondary {
            background: #8b5cf6;
            color: white;
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
          }
        `}
        id="card-secondary-styles"
      />

      <InjectStyles
        styles={`
          .card-success {
            background: #10b981;
            color: white;
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
          }
        `}
        id="card-success-styles"
      />

      <div className="card-primary">Карточка с основным цветом</div>
      <div className="card-secondary">Карточка со вторичным цветом</div>
      <div className="card-success">Карточка с цветом успеха</div>
    </>
  ),
};

/**
 * Пример с CSS анимациями
 */
export const WithAnimations: Story = {
  render: () => (
    <>
      <InjectStyles
        styles={`
          @keyframes slideIn {
            from {
              transform: translateX(-100%);
              opacity: 0;
            }
            to {
              transform: translateX(0);
              opacity: 1;
            }
          }
          
          @keyframes pulse {
            0%, 100% {
              transform: scale(1);
            }
            50% {
              transform: scale(1.05);
            }
          }
          
          .animated-box {
            background: linear-gradient(45deg, #f093fb 0%, #f5576c 100%);
            color: white;
            padding: 20px;
            margin: 10px 0;
            border-radius: 8px;
            animation: slideIn 0.5s ease-out, pulse 2s ease-in-out infinite;
          }
        `}
        id="animation-demo-styles"
      />

      <div className="animated-box">
        Анимированный блок с использованием внедренных стилей
      </div>
    </>
  ),
};

/**
 * Пример с медиа-запросами
 */
export const WithMediaQueries: Story = {
  render: () => (
    <>
      <InjectStyles
        styles={`
          .responsive-box {
            background: #6366f1;
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
          }
          
          @media (max-width: 768px) {
            .responsive-box {
              background: #ec4899;
              font-size: 14px;
            }
            .responsive-box::after {
              content: " (мобильная версия)";
            }
          }
          
          @media (min-width: 769px) and (max-width: 1024px) {
            .responsive-box {
              background: #f59e0b;
              font-size: 16px;
            }
            .responsive-box::after {
              content: " (планшет)";
            }
          }
          
          @media (min-width: 1025px) {
            .responsive-box {
              background: #10b981;
              font-size: 18px;
            }
            .responsive-box::after {
              content: " (десктоп)";
            }
          }
        `}
        id="responsive-demo-styles"
      />

      <div className="responsive-box">
        Адаптивный блок (измените размер окна)
      </div>
    </>
  ),
};
