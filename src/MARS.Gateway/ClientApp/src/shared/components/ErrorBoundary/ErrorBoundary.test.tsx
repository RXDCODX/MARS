import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { ErrorBoundary } from "./ErrorBoundary";

/**
 * Граница ошибок.
 *
 * Класс был написан, но не смонтирован нигде: импортов не было, кроме самого
 * файла. При этом три комментария в коде разбора и стора ссылались на неё как на
 * работающую защиту. Проверка без неё была и оставалась проверкой комментария.
 */
describe("граница ошибок", () => {
  const Boom = (): never => {
    throw new Error("взрыв рендера");
  };

  it("падение в рендере не уносит документ, а показывает заглушку", () => {
    // React логирует ошибку в консоль, поэтому она заглушается: проверяется
    // поведение границы, а не вывод.
    const consoleError = console.error;

    console.error = () => undefined;

    try {
      render(
        <ErrorBoundary>
          <Boom />
        </ErrorBoundary>
      );
    } finally {
      console.error = consoleError;
    }

    expect(screen.getByText("Что-то пошло не так")).toBeTruthy();
  });

  it("без ошибки показывает содержимое, а не заглушку", () => {
    render(
      <ErrorBoundary>
        <span>экран работает</span>
      </ErrorBoundary>
    );

    expect(screen.getByText("экран работает")).toBeTruthy();
  });

  it("заглушку можно заменить", () => {
    const consoleError = console.error;

    console.error = () => undefined;

    try {
      render(
        <ErrorBoundary fallback={<span>свой заглушечный экран</span>}>
          <Boom />
        </ErrorBoundary>
      );
    } finally {
      console.error = consoleError;
    }

    expect(screen.getByText("свой заглушечный экран")).toBeTruthy();
  });
});
