namespace MARS.Telegram.Domain.Entities;

/// <summary>Состояние отложенной публикации. Значения совпадают с монолитом.</summary>
public enum BooruScheduledPostStatus
{
    Pending = 0,
    Posted = 1,
    Failed = 2,
    Cancelled = 3,
}
