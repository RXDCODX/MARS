import { LucideIcon } from "lucide-react";

import styles from "./ErrorPage.module.scss"; // Импортируем стили

interface ErrorPageProperties {
  typeError: string;
  icon?: LucideIcon;
}

export const ErrorPage = ({ typeError, icon: Icon }: ErrorPageProperties) => {
  const errorMessages: { [key: string]: string } = {
    "404": "Страница не найдена",
    "500": "Внутренняя ошибка сервера",
  };

  const message = errorMessages[typeError] || "Неизвестная ошибка";

  return (
    <div className={styles["error-page"]}>
      {Icon && <Icon className={styles["error-icon"]} />}
      <h1 className={styles["error-title"]}>{message}</h1>
      <p className={styles["error-description"]}>
        Простите, произошла ошибка. Пожалуйста, попробуйте позже.
      </p>
      <button
        className={styles["error-button"]}
        onClick={() => (location.pathname = "/")}
      >
        Перезагрузить страницу
      </button>
    </div>
  );
};

ErrorPage.displayName = "ErrorPage";
