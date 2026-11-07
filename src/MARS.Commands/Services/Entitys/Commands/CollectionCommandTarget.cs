using MARS.Platform.Clients.Http.Infrastructure;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Пользователь, для которого команда показывает коллекцию.
/// </summary>
/// <remarks>
/// <c>TwitchId</c> и <c>Login</c> заполнены не одновременно: если логин
/// передали, но пользователя в базе нет, вернётся только логин — по нему
/// команда сообщает «не найден». Иначе были бы неразличимы случаи
/// «пользователь не передан» и «пользователя нет».
/// </remarks>
internal sealed record CollectionCommandTarget(
    string? TwitchId,
    string? DisplayName,
    string? Login
);
