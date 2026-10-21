using System.Globalization;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Grpc.TelegramLog;

namespace MARS.Telegram.Services.Logging;

/// <summary>
/// Формирование текста записи журнала для чата.
/// </summary>
/// <remarks>
/// <para>
/// Формат задан тем, что запись читает человек в чате: без уровня и без имени
/// сервиса непонятно, чья это ошибка, а без времени — когда она была.
/// </para>
/// <para>
/// Время печатается в UTC: локальное время машины владельца не значит
/// ничего для того, кто читает лог на другой машине.
/// </para>
/// <para>
/// Лимит Telegram — 4096 символов, и сообщение длиннее него не уходит вовсе.
/// Обрезка идёт до сборки финальной строки: заголовок и счётчик потерь важнее
/// хвоста стектрейса, и терять их нельзя.
/// </para>
/// </remarks>
public static class TelegramLogFormatter
{
    /// <summary>Лимит длины сообщения в Telegram.</summary>
    public const int MaxMessageLength = 4096;

    private const string Separator = "\n\n";

    /// <summary>
    /// Собирает текст записи.
    /// </summary>
    public static string Format(LogEntryRequest? request)
    {
        var result = string.Empty;

        var header = Header(request);
        var tail = Tail(request);
        var body = Body(request);

        // Место под тело считается после заголовка и хвоста: иначе обрезка
        // съела бы именно то, что в сообщении самое важное.
        var available = MaxMessageLength - header.Length - tail.Length - Separator.Length;

        if (available < 0)
        {
            available = 0;
        }

        if (body.Length > available)
        {
            body = available > 0 ? string.Concat(body.AsSpan(0, available - 1), "…") : string.Empty;
        }

        result = string.Concat(header, Separator, body, tail);

        if (result.Length > MaxMessageLength)
        {
            result = result[..MaxMessageLength];
        }

        return result;
    }

    /// <summary>
    /// Заголовок: знак уровня, уровень, имя сервиса, время и категория.
    /// </summary>
    private static string Header(LogEntryRequest? request)
    {
        var result = new StringBuilder();

        result.Append(Emoji(request?.Level));
        result.Append(' ');
        result.Append(LevelText(request?.Level));
        result.Append(" · ");
        result.Append(
            string.IsNullOrWhiteSpace(request?.Service) ? "без сервиса" : request.Service
        );

        var line = TimestampText(request?.Timestamp);
        var category = request?.Category ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(line) && !string.IsNullOrWhiteSpace(category))
        {
            result.Append('\n');
            result.Append(line);
            result.Append(" · ");
            result.Append(category);
        }
        else if (!string.IsNullOrWhiteSpace(line))
        {
            result.Append('\n');
            result.Append(line);
        }
        else if (!string.IsNullOrWhiteSpace(category))
        {
            result.Append('\n');
            result.Append(category);
        }

        return result.ToString();
    }

    /// <summary>
    /// Тело: текст записи и стектрейс исключения.
    /// </summary>
    private static string Body(LogEntryRequest? request)
    {
        var result = string.Empty;

        var message = request?.Message ?? string.Empty;
        var exception = request?.Exception ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(exception))
        {
            result = string.IsNullOrWhiteSpace(message)
                ? exception
                : string.Concat(message, Separator, exception);
        }
        else
        {
            result = message;
        }

        return result;
    }

    /// <summary>
    /// Хвост: счётчик локальных потерь отправителя.
    /// </summary>
    private static string Tail(LogEntryRequest? request)
    {
        var result = string.Empty;

        var suppressed = request?.SuppressedCount ?? 0;

        if (suppressed > 0)
        {
            result = string.Concat(
                Separator,
                string.Format(CultureInfo.InvariantCulture, "(+{0} подавлено)", suppressed)
            );
        }

        return result;
    }

    /// <summary>
    /// Знак уровня: без него записи Error и Fatal не отличить друг от друга.
    /// </summary>
    private static string Emoji(string? level) =>
        (level ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "VERBOSE" => "🔍",
            "DEBUG" => "🐛",
            "INFORMATION" => "ℹ️",
            "WARNING" => "⚠️",
            "ERROR" => "❌",
            "FATAL" => "💀",
            _ => "•",
        };

    /// <summary>
    /// Название уровня. Пустое значение означает «уровень неизвестен», а не
    /// «уровень Fatal».
    /// </summary>
    private static string LevelText(string? level)
    {
        var result = string.IsNullOrWhiteSpace(level) ? "Запись" : level.Trim();

        return result;
    }

    /// <summary>
    /// Время записи в UTC. Незаданное время не подменяется текущим: подмена
    /// означала бы «ошибка была только что», а это неправда.
    /// </summary>
    private static string TimestampText(Timestamp? timestamp)
    {
        var result = string.Empty;

        if (timestamp is not null)
        {
            var moment = timestamp.ToDateTimeOffset();

            // Нулевое время — это «время не задано», а не «ошибка была в 1970
            // году»: подменять его текущим нельзя, потому что запись пришла
            // из другого сервиса и время могло не дойти.
            if (moment != DateTimeOffset.UnixEpoch)
            {
                result = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss} UTC",
                    moment.ToUniversalTime()
                );
            }
        }

        return result;
    }
}
