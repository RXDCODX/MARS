namespace MARS.Telegram.Entities;

/// <summary>Источник изображений для автопостинга. Значения совпадают с монолитом.</summary>
public enum BooruSource
{
    Danbooru = 0,
    Rule34 = 1,
}

/// <summary>Площадка публикации. Значения совпадают с монолитом.</summary>
public enum BooruTargetPlatform
{
    Discord = 0,
    Telegram = 1,
}

/// <summary>Разбор разметки сообщения Telegram. Значения совпадают с монолитом.</summary>
public enum BooruTelegramParseMode
{
    Default = 0,
    Html = 1,
    Markdown = 2,
}

/// <summary>Состояние отложенной публикации. Значения совпадают с монолитом.</summary>
public enum BooruScheduledPostStatus
{
    Pending = 0,
    Posted = 1,
    Failed = 2,
    Cancelled = 3,
}
