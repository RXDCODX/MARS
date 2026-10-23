using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Настройки доставки журнала в Telegram.
/// </summary>
/// <remarks>
/// <para>
/// Ключи лежат в секции <c>Telegram</c> рядом с настройками самого бота: секция
/// общая для всех сервисов, и отдельная вложенная секция только размножила бы
/// путаницу между «настройками бота» и «настройками доставки журнала».
/// </para>
/// <para>
/// Имена свойств совпадают с ключами конфигурации, поэтому в переменной
/// окружения это <c>Telegram__LogEnabled</c> и <c>Telegram__LogMinimumLevel</c>.
/// </para>
/// </remarks>
public sealed class TelegramLogOptions
{
    public const string EnabledKey = "Telegram:LogEnabled";
    public const string MinimumLevelKey = "Telegram:LogMinimumLevel";
    public const string QueueCapacityKey = "Telegram:LogQueueCapacity";
    public const string MaxAttemptsKey = "Telegram:LogMaxAttempts";

    public const bool DefaultEnabled = true;
    public const string DefaultMinimumLevel = "Error";
    public const int DefaultQueueCapacity = 256;
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// Включена ли доставка. По умолчанию включена: недоставленная ошибка
    /// стоит дороже лишнего сообщения в чат.
    /// </summary>
    public bool Enabled { get; set; } = DefaultEnabled;

    /// <summary>
    /// Минимальный уровень записи. По умолчанию <c>Error</c>: предупреждения
    /// идут десятками, а чат для них — не место.
    /// </summary>
    public string MinimumLevel { get; set; } = DefaultMinimumLevel;

    /// <summary>
    /// Ёмкость очереди записей. Переполнение означает потерю: поэтому очередь
    /// конечна, и потеря считается, а не остаётся незамеченной.
    /// </summary>
    public int QueueCapacity { get; set; } = DefaultQueueCapacity;

    /// <summary>
    /// Сколько попыток доставки одной записи.
    /// </summary>
    public int MaxAttempts { get; set; } = DefaultMaxAttempts;

    /// <summary>
    /// Читает настройки из конфигурации. Мусор в значении не роняет сервис:
    /// неразобранное значение заменяется значением по умолчанию.
    /// </summary>
    public static TelegramLogOptions FromConfiguration(IConfiguration configuration)
    {
        var result = new TelegramLogOptions();

        if (configuration is not null)
        {
            result.Enabled = ReadBool(configuration, EnabledKey, DefaultEnabled);
            result.MinimumLevel = configuration[MinimumLevelKey] ?? DefaultMinimumLevel;
            result.QueueCapacity = Math.Max(
                1,
                ReadInt(configuration, QueueCapacityKey, DefaultQueueCapacity)
            );
            result.MaxAttempts = Math.Max(
                1,
                ReadInt(configuration, MaxAttemptsKey, DefaultMaxAttempts)
            );
        }

        return result;
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool fallback)
    {
        var result = fallback;
        var raw = configuration[key];

        if (!string.IsNullOrWhiteSpace(raw))
        {
            var value = raw.Trim();
            if (bool.TryParse(value, out var parsed))
            {
                result = parsed;
            }
            else if (value == "1")
            {
                result = true;
            }
            else if (value == "0")
            {
                result = false;
            }
        }

        return result;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        var result = fallback;
        var raw = configuration[key];

        if (
            !string.IsNullOrWhiteSpace(raw)
            && int.TryParse(
                raw.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed
            )
        )
        {
            result = parsed;
        }

        return result;
    }
}
