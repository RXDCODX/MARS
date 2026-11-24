namespace MARS.Telegram.Domain.Entities;

/// <summary>Разбор разметки сообщения Telegram. Значения совпадают с монолитом.</summary>
public enum BooruTelegramParseMode
{
    Default = 0,
    Html = 1,
    Markdown = 2,
}
