using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Tests.Services;

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
    public Task<TwitchUser> EnsureUserExistsAsync(
        ChatMessage chatMessage,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(TwitchUser.FromChatMessage(chatMessage), cancellationToken);

    public Task<TwitchUser> EnsureUserExistsAsync(
        OnMessageReceivedArgs args,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(TwitchUser.FromOnMessageReceivedArgs(args), cancellationToken);

    public Task<TwitchUser> EnsureUserExistsAsync(
        ChannelPointsCustomRewardRedemptionArgs args,
        CancellationToken cancellationToken = default
    ) =>
        EnsureAsync(
            TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(args),
            cancellationToken
        );

    public Task<TwitchUser> EnsureUserExistsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(User(twitchId), cancellationToken);

    public Task<TwitchUser> EnsureUserExistsAsync(
        TwitchUser? twitchUser,
        CancellationToken cancellationToken = default
    ) => EnsureAsync(twitchUser, cancellationToken);

    public Task<TwitchUser?> EnsureUserExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("В этом тесте поиск по логину не используется");

    private async Task<TwitchUser> EnsureAsync(
        TwitchUser? twitchUser,
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

        var created = new TwitchUser
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

    private static TwitchUser User(string twitchId) =>
        new()
        {
            TwitchId = twitchId,
            UserLogin = "login",
            DisplayName = "Pyro",
        };
}
