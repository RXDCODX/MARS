import { useToastModal } from "@/shared/Utils/ToastModal";
import { messageOf } from "@/shared/types/OperationResult";

import { describeLogsProbe } from "../logsProbe";
interface LogsTestButtonsProperties {
  onLogsRefresh: () => void;
  disabled?: boolean;
}

const LogsTestButtons: React.FC<LogsTestButtonsProperties> = ({
  onLogsRefresh,
  disabled = false,
}) => {
  const { showToast } = useToastModal();

  const handleCreateTestLogs = async () => {
    try {
      const response = await fetch("/api/Logs/test", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      });

      // `response.ok` здесь всегда истинна: эндпоинта `/api/Logs/test` в проекте
      // нет, а catch-all клиента отвечает на любой путь `index.html` с кодом 200.
      // Проверка `ok` рапортовала бы об успехе, хотя ничего не создано, поэтому
      // отличается тело ответа, а не код.
      const probe = describeLogsProbe(
        response.headers.get("content-type") ?? "",
        await response.text()
      );

      if (probe.kind === "spa") {
        showToast({
          success: false,
          message: probe.message,
        });

        return;
      }

      showToast({
        success: true,
        message: "Тестовые логи созданы. Проверьте таблицу логов",
      });
      // Обновляем логи после создания тестовых
      setTimeout(() => onLogsRefresh(), 1000);
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(error, "Неизвестная ошибка"),
      });
    }
  };

  const handleCheckStatistics = async () => {
    try {
      const response = await fetch("/api/Logs/statistics");

      // Как и в соседней кнопке: catch-all отвечает 200 с `index.html`, поэтому
      // `response.ok` истинна, а `response.json()` бросает `SyntaxError`, и
      // пользователь после клика не видел вообще ничего — ни тоста, ни ошибки.
      const probe = describeLogsProbe(
        response.headers.get("content-type") ?? "",
        await response.text()
      );

      if (probe.kind === "spa") {
        showToast({
          success: false,
          message: probe.message,
        });

        return;
      }

      const stats = JSON.parse(await response.text()) as {
        totalLogs?: number;
        errorLogs?: number;
        warningLogs?: number;
      };

      showToast({
        success: true,
        message: `Статистика логов — всего: ${stats.totalLogs ?? 0}, ошибок: ${stats.errorLogs ?? 0}, предупреждений: ${stats.warningLogs ?? 0}`,
      });
    } catch (error) {
      console.error("Ошибка получения статистики:", error);
      showToast({
        success: false,
        message: messageOf(error, "Не удалось получить статистику логов"),
      });
    }
  };

  return (
    <div className="mb-3">
      <button
        className="btn btn-warning me-2"
        onClick={handleCreateTestLogs}
        disabled={disabled}
      >
        Создать тестовые логи
      </button>

      <button
        className="btn btn-info"
        onClick={handleCheckStatistics}
        disabled={disabled}
      >
        Проверить статистику
      </button>
    </div>
  );
};

export default LogsTestButtons;
