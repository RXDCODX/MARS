using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Grpc.TelegramLog;
using MARS.Telegram.Services.Logging;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Формирование текста записи журнала для чата.
///
/// Формат задан тем, что запись читает человек в чате: без уровня и без имени
/// сервиса непонятно, чья это ошибка, а без времени — когда она была. Время
/// печатается в UTC: локальное время машины владельца не значит ничего для
/// того, кто читает лог на другой машине.
///
/// Отдельная проверка — длина: у Telegram жёсткий лимит в 4096 символов, и
/// сообщение длиннее него не уходит вовсе.
/// </summary>
public class TelegramLogFormatterTests
{
    /// <summary>
    /// В заголовке — уровень и имя сервиса: без них запись в потоке сообщений
    /// неотличима от любой другой.
    /// </summary>
    [Fact]
    public void Заголовок_Содержит_Уровень_И_Сервис()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = "Что-то сломалось",
            }
        );

        Assert.Contains("Error", text);
        Assert.Contains("MARS.TwitchCore", text);
        Assert.Contains("❌", text);
    }

    /// <summary>
    /// У Fatal свой знак: он отличается от Error только знаком и названием, а
    /// иначе падение и ошибка выглядели бы одинаково.
    /// </summary>
    [Fact]
    public void У_Уровня_Fatal_Свой_Знак()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Fatal",
                Message = "Процесс упал",
            }
        );

        Assert.Contains("💀", text);
        Assert.DoesNotContain("❌", text);
    }

    /// <summary>
    /// Время печатается в UTC: локальное время машины владельца не значит
    /// ничего для того, кто читает лог на другой машине.
    /// </summary>
    [Fact]
    public void Время_Печатается_В_Utc()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = "Что-то сломалось",
                Timestamp = Timestamp.FromDateTimeOffset(
                    new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero)
                ),
            }
        );

        Assert.Contains("2026-10-21 12:00:00 UTC", text);
    }

    /// <summary>
    /// Категория (SourceContext) показывается: она отвечает на вопрос «где
    /// именно сломалось».
    /// </summary>
    [Fact]
    public void Категория_Попадает_В_Текст()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Category = "MARS.TwitchCore.Services.Rewards",
                Message = "Что-то сломалось",
            }
        );

        Assert.Contains("MARS.TwitchCore.Services.Rewards", text);
    }

    /// <summary>
    /// Исключение со стектрейсом — половина сведений об ошибке: без него
    /// видно только «что-то сломалось».
    /// </summary>
    [Fact]
    public void Исключение_Добавляется_К_Тексту()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = "Награда не выдалась",
                Exception = "System.InvalidOperationException: причина",
            }
        );

        Assert.Contains("Награда не выдалась", text);
        Assert.Contains("System.InvalidOperationException: причина", text);
    }

    /// <summary>
    /// Локальные потери отправителя видны в чате: молчаливая потеря выглядела
    /// бы как «доставилось всё».
    /// </summary>
    [Fact]
    public void Подавленные_Записи_Показываются_Счётчиком()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = "Что-то сломалось",
                SuppressedCount = 3,
            }
        );

        Assert.Contains("+3 подавлено", text);
    }

    /// <summary>
    /// Без потерь счётчика нет: строка «(+0 подавлено)» в каждом сообщении
    /// только шумит.
    /// </summary>
    [Fact]
    public void Без_Потерь_Счётчика_Нет()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = "Что-то сломалось",
            }
        );

        Assert.DoesNotContain("подавлено", text);
    }

    /// <summary>
    /// Длинная запись обрезается до лимита Telegram: сообщение длиннее 4096
    /// символов не уходит вовсе, то есть ошибка была бы потеряна целиком.
    /// </summary>
    [Fact]
    public void Длинная_Запись_Обрезается_До_Лимита_Telegram()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Message = new string('x', 10_000),
            }
        );

        Assert.True(
            text.Length <= TelegramLogFormatter.MaxMessageLength,
            $"Длина сообщения {text.Length} больше лимита Telegram"
        );
        Assert.Contains("MARS.TwitchCore", text);
    }

    /// <summary>
    /// Обрезка не должна съесть заголовок: потерянный заголовок превращает
    /// сообщение в бессмысленный хвост.
    /// </summary>
    [Fact]
    public void Обрезка_Не_Съедает_Заголовок()
    {
        var text = TelegramLogFormatter.Format(
            new LogEntryRequest
            {
                Service = "MARS.TwitchCore",
                Level = "Error",
                Category = "MARS.TwitchCore.Services.Rewards",
                Message = new string('x', 10_000),
                Exception = new string('y', 10_000),
            }
        );

        Assert.Contains("Error", text);
        Assert.Contains("MARS.TwitchCore.Services.Rewards", text);
        Assert.True(text.Length <= TelegramLogFormatter.MaxMessageLength);
    }
}
