namespace MARS.Telegram.Entities;

/// <summary>
/// Отметка о публикации изображения в канал Discord.
/// </summary>
/// <remarks>
/// Ключ дедупликации — тройка «источник + id изображения + канал»: без неё
/// автопостинг при каждом проходе планировщика публиковал бы одно и то же
/// изображение снова.
/// </remarks>
public class PostedImageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Source { get; set; } = string.Empty;

    public int ImageId { get; set; }

    public ulong DiscordChannelId { get; set; }

    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;
}
