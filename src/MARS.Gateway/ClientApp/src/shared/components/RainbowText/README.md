# RainbowText

Компонент для отображения текста с анимированным градиентом.

## Реализация

Этот компонент является реэкспортом компонента `GradientText` из библиотеки [react-bits](https://github.com/DavidHDev/react-bits).

## Использование

```tsx
import RainbowText from "@/shared/components/RainbowText/RainbowText";

// Базовое использование
<RainbowText>Привет, мир!</RainbowText>

// С кастомными цветами
<RainbowText colors={["#ff0080", "#ff8c00", "#40e0d0"]}>
  Кастомные цвета
</RainbowText>

// С рамкой
<RainbowText showBorder>
  Текст с градиентной рамкой
</RainbowText>

// С настройкой скорости анимации
<RainbowText animationSpeed={3}>
  Быстрая анимация
</RainbowText>
```

## Props

| Prop             | Type        | Default                                                   | Описание                                   |
| ---------------- | ----------- | --------------------------------------------------------- | ------------------------------------------ |
| `children`       | `ReactNode` | -                                                         | Текст для отображения (обязательный)       |
| `className`      | `string`    | `""`                                                      | Дополнительные CSS классы                  |
| `colors`         | `string[]`  | `["#40ffaa", "#4079ff", "#40ffaa", "#4079ff", "#40ffaa"]` | Массив цветов для градиента                |
| `animationSpeed` | `number`    | `8`                                                       | Скорость анимации в секундах               |
| `showBorder`     | `boolean`   | `false`                                                   | Показывать градиентную рамку вокруг текста |

## Примеры в Storybook

Компонент имеет несколько примеров использования в Storybook:

- Default - базовое использование
- LongText - длинный текст
- ShortText - короткий текст
- WithBorder - с градиентной рамкой
- WithEmojis - с эмодзи
- CustomColors - с кастомными цветами
- FastAnimation - быстрая анимация
- SlowAnimation - медленная анимация

Для просмотра примеров запустите:

```bash
yarn storybook
```

## История изменений

- **2025-10-08**: Переход на использование компонента из библиотеки react-bits для упрощения поддержки и получения обновлений
