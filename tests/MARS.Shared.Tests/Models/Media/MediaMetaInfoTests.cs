using System.ComponentModel.DataAnnotations;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Tests.Models.Media;

/// <summary>
/// Проверка метаданных медиа.
/// </summary>
/// <remarks>
/// <see cref="MediaMetaInfo"/> реализует <see cref="IValidatableObject"/>, и
/// проверка вызывается при разборе запроса. Без неё заморозка при обычном
/// приоритете уехала бы в оверлей: приоритет <c>High</c> поднимает её над
/// остальными алертами, а обычный — нет.
/// </remarks>
public class MediaMetaInfoTests
{
    /// <summary>
    /// Заморозка при обычном приоритете — ошибка: она не сработает, как
    /// задумано, и об этом должен узнать автор медиа, а не зритель.
    /// </summary>
    [Fact]
    public void Заморозка_При_Обычном_Приоритете_Ошибка()
    {
        var info = new MediaMetaInfo
        {
            DisplayName = "медиа",
            IsFreezeRequired = true,
            Priority = MediaAlertPriority.Normal,
        };

        var errors = info.Validate(new ValidationContext(info)).ToList();

        var error = Assert.Single(errors);
        Assert.Contains(nameof(MediaMetaInfo.IsFreezeRequired), error.MemberNames);
        Assert.Contains(nameof(MediaMetaInfo.Priority), error.MemberNames);
    }

    /// <summary>
    /// Заморозка при высоком приоритете — законное состояние, ошибок быть не
    /// должно: иначе медиа с заморозкой нельзя было бы сохранить вовсе.
    /// </summary>
    [Fact]
    public void Заморозка_При_Высоком_Приоритете_Ошибок_Нет()
    {
        var info = new MediaMetaInfo
        {
            DisplayName = "медиа",
            IsFreezeRequired = true,
            Priority = MediaAlertPriority.High,
        };

        Assert.Empty(info.Validate(new ValidationContext(info)));
    }

    /// <summary>
    /// Без заморозки проверка не срабатывает никогда — иначе обычное медиа
    /// отсеивалось бы при каждом сохранении.
    /// </summary>
    [Fact]
    public void Без_Заморозки_Ошибок_Нет()
    {
        var info = new MediaMetaInfo { DisplayName = "медиа" };

        Assert.Empty(info.Validate(new ValidationContext(info)));
    }
}
