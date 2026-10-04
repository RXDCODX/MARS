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
      if (response.ok) {
        const stats = await response.json();
        console.log("Статистика логов:", stats);
        showToast({
          success: true,
          message: `Статистика логов - Всего: ${stats.totalLogs}, Ошибок: ${stats.errorLogs}, Предупреждений: ${stats.warningLogs}`,
        });
      }
    } catch (error) {
      console.error("Ошибка получения статистики:", error);
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
