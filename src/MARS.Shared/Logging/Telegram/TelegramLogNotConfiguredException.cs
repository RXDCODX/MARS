namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Telegram не настроен принимать журнал: не задан токен бота или нет ни одного
/// адресата.
/// </summary>
/// <remarks>
/// Ошибка permanent: повторять доставку столько же, сколько записей пришло,
/// бессмысленно — настройка не появится сама. Отправитель отключает доставку и
/// пишет об этом один раз в stderr.
/// </remarks>
public sealed class TelegramLogNotConfiguredException(string message) : Exception(message);
