import { Component, ErrorInfo, ReactNode } from "react";

interface Properties {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Properties, State> {
  public state: State = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Uncaught error:", error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      return (
        this.props.fallback || (
          // `data-testid` — не украшение, а признак падения рендера для E2E.
          // Раньше единственным признаком была запись `console.error` в
          // `componentDidCatch`, то есть проверка молча зависела от того, что
          // этот вызов не уберут: уберут — и набор тестов останется зелёным,
          // ничего не проверяя. Опора на текст заголовка была бы хуже: он
          // меняется вместе с формулировкой.
          <div className="error-boundary" data-testid="error-boundary">
            <h2>Что-то пошло не так</h2>
            <p>Произошла ошибка в приложении. Пожалуйста, обновите страницу.</p>
            <button onClick={() => location.reload()}>Обновить страницу</button>
          </div>
        )
      );
    }

    return this.props.children;
  }
}
