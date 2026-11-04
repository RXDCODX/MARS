# HighliteMessage Component

Компонент для отображения всплывающих сообщений с лицами из папки ассетов.

## Описание

HighliteMessage - это система всплывающих сообщений, которая отображает чат-сообщения вместе с случайно выбранными лицами (изображениями или видео) из папки `src/assets/faces/`.

## Особенности

- **Автоматический выбор лиц**: Каждое сообщение получает случайное лицо из доступных ассетов
- **Поддержка изображений и видео**: Работает как с GIF изображениями, так и с MP4 видео
- **Анимации**: Включает плавные анимации появления и исчезновения
- **Адаптивный текст**: Текст автоматически подстраивается под размер контейнера
- **Градиентные фоны**: Красивые градиентные фоны для текстовых пузырьков

## Структура файлов

```text
HighliteMessage/
├── HighliteMessage.tsx      # Основной компонент
├── Message.tsx              # Компонент сообщения
├── MessageDemo.tsx          # Демо-компонент для Storybook
├── Message.module.scss      # Стили
├── HighliteMessage.stories.tsx  # Storybook истории
├── index.ts                 # Экспорты
└── README.md               # Документация
```

## Использование

### Базовое использование

```tsx
import { HighliteMessage } from "@/components/OBS_Components";

function App() {
  return <HighliteMessage />;
}
```

### С SignalR

Компонент автоматически подключается к SignalR хабу "Highlite" и отображает сообщения:

```tsx
// SignalR отправляет: message, color
// Компонент автоматически генерирует случайное лицо
```

## Доступные лица

Компонент использует лица из папки `src/assets/faces/`:

### Изображения (GIF)

- `3d-saul-saul-goodman.gif`
- `blank-stare-really.gif`
- `blue-archive-arisu.gif`
- `clash-royale.gif`
- `cristiano-ronaldo-soccer.gif`
- `devil-may-cry-dmc.gif`
- `dono-wall.gif`
- `ferass18.gif`
- `funny-dogs-cute.gif`
- `giga-chad.gif`
- `homelander-milk.gif`
- `homelander-the-boys.gif`
- `marin-kitagawa.gif`
- `mika-misono-mika.gif`
- `plink-cat-plink.gif`
- `sus-suspicious.gif`
- `tachibana-hikari-blue-archive.gif`
- `tendou-aris-blue-archive.gif`
- `vargillllll-vargil.gif`

### Видео (MP4)

- `1233233.gif.mp4`
- `animation.gif.mp4`
- `dante-dante-devil-may-cry.mp4`
- `video_2025-02-02_17-11-44.mp4`

## API

### HighliteMessageProps

```tsx
interface HighliteMessageProps {
  message: ChatMessage; // Сообщение чата
  color: string; // Цвет пользователя
  faceImage: FaceAsset; // Лицо для отображения
}
```

### FaceAsset

```tsx
interface FaceAsset {
  name: string; // Имя файла
  url: string; // Путь к файлу
  type: "image" | "video"; // Тип медиа
  extension: string; // Расширение файла
}
```

## Утилиты

### getRandomFace()

Возвращает случайное лицо из всех доступных ассетов.

### getRandomFaceByType(type: 'image' | 'video')

Возвращает случайное лицо определенного типа.

### getFaceByName(name: string)

Возвращает лицо по имени файла.

## Стилизация

Компонент использует CSS модули с переменными:

```scss
:root {
  --color: #ff0000 #ff0000 transparent transparent;
  --calculated-height: calc(100vh / 29);
  --span-width: 100ch;
}
```

## Storybook

Компонент включает несколько Storybook историй:

- **Default**: Базовое отображение
- **Empty**: Пустое состояние
- **Demo**: Демонстрация с тестовыми сообщениями
- **ImagesOnly**: Только изображения
- **VideosOnly**: Только видео

## Тестирование

Компонент включает автоматические тесты в Storybook:

- Проверка рендеринга
- Проверка появления сообщений
- Проверка загрузки медиа
- Проверка анимаций

## Зависимости

- `react-textfit` - для адаптивного текста
- `@/shared/Utils` - для утилит работы с лицами
- `@/shared/styles/animate.module.scss` - для анимаций
