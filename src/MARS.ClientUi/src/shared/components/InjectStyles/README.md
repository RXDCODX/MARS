# InjectStyles - Компонент для внедрения стилей в head

Компонент и хук для динамического внедрения CSS стилей в `<head>` страницы при рендеринге компонента.

## Использование

### Вариант 1: Компонент InjectStyles

```tsx
import { InjectStyles } from "@/shared/components/InjectStyles";

function MyComponent() {
  return (
    <>
      <InjectStyles
        styles={`
          .my-custom-class {
            background-color: #ff0000;
            color: white;
            padding: 10px;
          }
          
          .another-class {
            font-size: 20px;
          }
        `}
        id="my-component-styles"
      />

      <div className="my-custom-class">
        Этот элемент использует внедренные стили
      </div>
    </>
  );
}
```

### Вариант 2: Хук useInjectStyles

```tsx
import { useInjectStyles } from "@/shared/hooks";

function MyComponent() {
  useInjectStyles(
    `
    .dynamic-class {
      background: linear-gradient(90deg, #ff0000, #00ff00);
    }
  `,
    "dynamic-styles"
  );

  return <div className="dynamic-class">Контент с динамическими стилями</div>;
}
```

### Динамические стили на основе пропсов

```tsx
import { InjectStyles } from "@/shared/components/InjectStyles";

interface MyComponentProps {
  primaryColor: string;
  fontSize: number;
}

function MyComponent({ primaryColor, fontSize }: MyComponentProps) {
  const dynamicStyles = `
    .themed-component {
      color: ${primaryColor};
      font-size: ${fontSize}px;
    }
  `;

  return (
    <>
      <InjectStyles styles={dynamicStyles} id="themed-component-styles" />

      <div className="themed-component">Компонент с динамической темой</div>
    </>
  );
}
```

## Параметры

### InjectStyles (Компонент)

| Параметр | Тип      | Обязательный | Описание                                                                                              |
| -------- | -------- | ------------ | ----------------------------------------------------------------------------------------------------- |
| `styles` | `string` | Да           | CSS строка со стилями для внедрения                                                                   |
| `id`     | `string` | Нет          | Уникальный идентификатор для style элемента. Если элемент с таким id уже существует, он будет заменен |

### useInjectStyles (Хук)

| Параметр | Тип      | Обязательный | Описание                                    |
| -------- | -------- | ------------ | ------------------------------------------- |
| `styles` | `string` | Да           | CSS строка со стилями для внедрения         |
| `id`     | `string` | Нет          | Уникальный идентификатор для style элемента |

## Особенности

- ✅ **Автоматическая очистка**: стили автоматически удаляются из head при размонтировании компонента
- ✅ **Предотвращение дублирования**: если указан `id`, существующий элемент с таким же id будет заменен
- ✅ **Типобезопасность**: полная поддержка TypeScript
- ✅ **Декларативный API**: можно использовать как компонент или как хук

## Когда использовать

- Для изоляции стилей, специфичных для одного компонента
- Для динамических стилей, зависящих от пропсов или состояния
- Для переопределения глобальных стилей в контексте конкретной страницы
- Для создания тем или кастомизации компонентов

## Примечания

- Стили применяются глобально, даже если компонент внедряет их локально
- При использовании `id`, убедитесь, что он уникален в пределах приложения
- Стили будут удалены при размонтировании компонента
