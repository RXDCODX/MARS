using MARS.TwitchCore.Extensions;

namespace MARS.TwitchCore.Services.Commands;

/// <summary>
/// Решает, является ли пишущий в чате администратором стенда.
///
/// Правило живёт здесь, а не в MARS.Commands, потому что только сервис платформы
/// знает личность: у него есть флаги из сообщения чата
/// (<c>ChatMessageEvent.IsModerator</c>, <c>IsBroadcaster</c>), и он передаёт
/// результат полем <c>is_admin</c>. В монолите то же решение было в
/// <c>TwitchCommandService.IsAdmin</c> как сверка со стримером.
///
/// Отличие от монолита: модератор тоже администратор. Там в командном фреймворке
/// был только стример, но валидаторы наград и <c>TwitchTitleChangeCommand</c>
/// допускали модератора — иначе команда <c>title</c> была бы недоступна тому,
/// ради кого и писалась.
/// </summary>
public static class TwitchCommandPermissions
{
    /// <summary>
    /// <paramref name="userId"/> — идентификатор пишущего из сообщения чата,
    /// <paramref name="isModerator"/> — флаг модератора из того же сообщения.
    /// </summary>
    public static bool IsAdmin(string? userId, bool isModerator)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var isBroadcaster = userId.Equals(
                TwitchConstants.ChannelId,
                StringComparison.OrdinalIgnoreCase
            );

            return isBroadcaster || isModerator;
        }

        return false;
    }
}
