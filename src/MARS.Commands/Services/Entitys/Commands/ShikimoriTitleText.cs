using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Форматирование ссылки на произведение Shikimori для команд
/// <c>randomanime</c> и <c>randommanga</c>.
/// </summary>
/// <remarks>
/// Русское название приоритетнее оригинального, год выводится знаком вопроса,
/// когда Shikimori его не знает: пустой круглый скелет в сообщении хуже
/// вопросительного знака.
/// </remarks>
internal static class ShikimoriTitleText
{
    public static string Format(ShikimoriTitleRef title)
    {
        var name = title.RussianName ?? title.Name ?? "?";
        var year = title.Year?.ToString() ?? "?";

        return $"{name} ({year} г.) — {title.Url}";
    }
}
