import { useToastModal } from "@/shared/Utils/ToastModal";

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

      if (response.ok) {
        showToast({
          success: true,
          message: "Тестовые логи созданы. Проверьте таблицу логов",
        });
        // Обновляем логи после создания тестовых
        setTimeout(() => onLogsRefresh(), 1000);
      } else {
        const errorText = await response.text();
        showToast({
          success: false,
          message: `Статус: ${response.status}, Ошибка: ${errorText}`,
        });
      }
    } catch (error) {
      showToast({
        success: false,
        message: error instanceof Error ? error.message : "Неизвестная ошибка",
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
