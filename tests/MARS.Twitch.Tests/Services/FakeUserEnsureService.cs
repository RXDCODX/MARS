using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.Twitch.Tests.Services;

/// <summary>
/// Заглушка сервиса «обеспечить пользователя», которая действительно пишет в базу.
///
/// Настоящая нужна не для логики: фоловер ссылается на пользователя внешним
/// ключом, и без записи пользователя EF не отдаёт строку фоловера даже с
/// Include. То есть пустая заглушка делала бы тесты проверкой несуществующей
/// схемы.
/// </summary>
internal sealed class FakeUserEnsureService(IDbContextFactory<TwitchDbContext> factory)
    : ITwitchUserEnsureService
{
    public Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureUserExistsAsync(
        ChatMessage chatMessage,
        CancellationToken cancellationToken = default
    ) =>
        EnsureAsync(
            MARS.Twitch.Domain.Entities.TwitchUser.FromChatMessage(chatMessage),
            cancellationToken
        );

    public Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureUserExistsAsync(
        OnMessageReceivedArgs args,
        CancellationToken cancellationToken = default
    ) =>
        EnsureAsync(
            MARS.Twitch.Domain.Entities.TwitchUser.FromOnMessageReceivedArgs(args),
            cancellationToken
        );

    public Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureUserExistsAsync(
        ChannelPointsCustomRewardRedemptionArgs args,
        CancellationToken cancellationToken = default
    ) =>
        EnsureAsync(
            MARS.Twitch.Domain.Entities.TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(
                args
            ),
            cancellationToken
        );

    public Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureUserExistsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(User(twitchId), cancellationToken);

    public Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureUserExistsAsync(
        MARS.Twitch.Domain.Entities.TwitchUser? twitchUser,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(twitchUser, cancellationToken);

    public Task<MARS.Twitch.Domain.Entities.TwitchUser?> EnsureUserExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("В этом тесте поиск по логину не используется");

    private async Task<MARS.Twitch.Domain.Entities.TwitchUser> EnsureAsync(
        MARS.Twitch.Domain.Entities.TwitchUser? twitchUser,
        CancellationToken cancellationToken
    )
    {
        if (twitchUser is null || string.IsNullOrWhiteSpace(twitchUser.TwitchId))
        {
            throw new ArgumentException("Пользователь не задан", nameof(twitchUser));
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.TwitchUsers.FirstOrDefaultAsync(
            user => user.TwitchId == twitchUser.TwitchId,
            cancellationToken
        );

        if (existing is not null)
        {
            return existing;
        }

        var created = new MARS.Twitch.Domain.Entities.TwitchUser
        {
            TwitchId = twitchUser.TwitchId,
            UserLogin = twitchUser.UserLogin,
            DisplayName = twitchUser.DisplayName,
            ProfileImageUrl = twitchUser.ProfileImageUrl,
            ChatColor = twitchUser.ChatColor,
            IsModerator = twitchUser.IsModerator,
            IsVip = twitchUser.IsVip,
        };
        db.TwitchUsers.Add(created);
        await db.SaveChangesAsync(cancellationToken);

        return created;
    }

    private static MARS.Twitch.Domain.Entities.TwitchUser User(string twitchId) =>
        new()
        {
            TwitchId = twitchId,
            UserLogin = "login",
            DisplayName = "Pyro",
        };
}
