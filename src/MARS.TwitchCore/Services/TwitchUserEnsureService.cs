using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Services;

public class TwitchUserEnsureService : ITwitchUserEnsureService
{
    private readonly IDbContextFactory<TwitchDbContext>? _dbFactory;
    private readonly TwitchUserInfoService? _userInfoService;
    private readonly TokenService? _tokenService;
    private readonly ITwitchAPI? _api;
    private readonly ILogger<TwitchUserEnsureService>? _logger;

    public TwitchUserEnsureService(
        IDbContextFactory<TwitchDbContext>? dbFactory = null,
        TwitchUserInfoService? userInfoService = null,
        TokenService? tokenService = null,
        ITwitchAPI? api = null,
        ILogger<TwitchUserEnsureService>? logger = null
    )
    {
        _dbFactory = dbFactory;
        _userInfoService = userInfoService;
        _tokenService = tokenService;
        _api = api;
        _logger = logger;
    }

    public TwitchUserEnsureService()
        : this(null) { }

    public virtual async Task<TwitchUser> EnsureUserExistsAsync(
        ChatMessage chatMessage,
        CancellationToken cancellationToken = default
    )
    {
        var twitchUser =
            TwitchUser.FromChatMessage(chatMessage)
            ?? throw new ArgumentException("Invalid ChatMessage data", nameof(chatMessage));
        return await EnsureUserExistsAsync(twitchUser, cancellationToken);
    }

    public virtual async Task<TwitchUser> EnsureUserExistsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            throw new ArgumentException("User not found", nameof(twitchId));
        }

        if (_dbFactory != null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db
                .TwitchUsers.AsNoTracking()
                .FirstOrDefaultAsync(u => u.TwitchId == twitchId, cancellationToken);
            if (existing != null)
            {
                return existing;
            }
        }

        if (_api != null && _tokenService?.Token?.AccessToken != null)
        {
            var response = await _api.Helix.Users.GetUsersAsync(
                [twitchId],
                null,
                _tokenService.Token.AccessToken
            );
            if (response?.Users?.Length > 0)
            {
                var twitchUser =
                    TwitchUser.FromUser(response.Users.First())
                    ?? throw new ArgumentException("Invalid TwitchId", nameof(twitchId));
                return await EnsureUserExistsAsync(twitchUser, cancellationToken);
            }
        }

        throw new ArgumentException("User not found", nameof(twitchId));
    }

    public virtual async Task<TwitchUser> EnsureUserExistsAsync(
        OnMessageReceivedArgs args,
        CancellationToken cancellationToken = default
    )
    {
        var twitchUser =
            TwitchUser.FromOnMessageReceivedArgs(args)
            ?? throw new ArgumentException("Invalid OnMessageReceivedArgs data", nameof(args));
        return await EnsureUserExistsAsync(twitchUser, cancellationToken);
    }

    public virtual async Task<TwitchUser> EnsureUserExistsAsync(
        ChannelPointsCustomRewardRedemptionArgs args,
        CancellationToken cancellationToken = default
    )
    {
        var twitchUser = TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(args);
        ArgumentNullException.ThrowIfNull(twitchUser);
        return await EnsureUserExistsAsync(twitchUser, cancellationToken);
    }

    public virtual async Task<TwitchUser> EnsureUserExistsAsync(
        TwitchUser? twitchUser,
        CancellationToken cancellationToken = default
    )
    {
        if (twitchUser == null || string.IsNullOrWhiteSpace(twitchUser.TwitchId))
        {
            return null!;
        }

        try
        {
            if (_dbFactory != null)
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var existing = await db.TwitchUsers.FindAsync(
                    [twitchUser.TwitchId],
                    cancellationToken: cancellationToken
                );
                if (existing != null)
                {
                    existing.UserLogin = twitchUser.UserLogin;
                    existing.DisplayName = twitchUser.DisplayName;
                    existing.ProfileImageUrl =
                        twitchUser.ProfileImageUrl ?? existing.ProfileImageUrl;
                    existing.ChatColor = twitchUser.ChatColor ?? existing.ChatColor;
                    existing.IsModerator = twitchUser.IsModerator;
                    existing.IsVip = twitchUser.IsVip;
                    existing.LastUpdated = DateTime.Now;
                    await db.SaveChangesAsync(cancellationToken);
                    _logger?.LogInformation(
                        "Обновлен пользователь Twitch: {UserName} (ID: {UserId})",
                        existing.UserLogin,
                        existing.TwitchId
                    );
                    return existing;
                }

                var enriched = await EnrichUserDataFromApiAsync(twitchUser);
                db.TwitchUsers.Add(enriched);
                await db.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation(
                    "Создан новый пользователь Twitch: {UserName} (ID: {UserId}), Avatar: {Avatar}",
                    enriched.UserLogin,
                    enriched.TwitchId,
                    enriched.ProfileImageUrl ?? "null"
                );
                return enriched;
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true)
        {
            _logger?.LogInformation(
                "Конфликт при создании пользователя {TwitchId}. Получение существующего пользователя.",
                twitchUser.TwitchId
            );
            if (_dbFactory != null)
            {
                await using var dbRetry = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var created = await dbRetry.TwitchUsers.FirstOrDefaultAsync(
                    u => u.TwitchId == twitchUser.TwitchId,
                    cancellationToken
                );
                if (created != null)
                {
                    return created;
                }
            }
            throw new InvalidOperationException(
                $"Не удалось получить пользователя {twitchUser.TwitchId} после constraint violation"
            );
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex);
        }
        return null!;
    }

    public virtual async Task<TwitchUser?> EnsureUserExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        var normalizedLogin = login.Trim().ToLowerInvariant();

        if (_dbFactory != null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db
                .TwitchUsers.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserLogin == normalizedLogin, cancellationToken);
            if (existing != null)
            {
                return existing;
            }
        }

        if (_api != null && _tokenService?.Token?.AccessToken != null)
        {
            try
            {
                var response = await _api.Helix.Users.GetUsersAsync(
                    null,
                    [normalizedLogin],
                    _tokenService.Token.AccessToken
                );
                if (response?.Users?.Length > 0)
                {
                    var twitchUser = TwitchUser.FromUser(response.Users.First());
                    if (twitchUser != null)
                    {
                        return await EnsureUserExistsAsync(twitchUser, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex);
            }
        }

        return null;
    }

    private async Task<TwitchUser> EnrichUserDataFromApiAsync(TwitchUser twitchUser)
    {
        if (_tokenService?.Token?.AccessToken == null)
        {
            return twitchUser;
        }

        try
        {
            var userInfoTask = _userInfoService!.GetUserInfoAsync(twitchUser.TwitchId);
            var chatColorTask = _userInfoService!.GetUserChatColorAsync(twitchUser.TwitchId);
            await Task.WhenAll(userInfoTask, chatColorTask);
            var apiUser = await userInfoTask;
            var chatColor = await chatColorTask;
            if (twitchUser.ProfileImageUrl == null && apiUser?.ProfileImageUrl != null)
            {
                twitchUser.ProfileImageUrl = apiUser.ProfileImageUrl;
            }

            if (twitchUser.ChatColor == null && chatColor != null)
            {
                twitchUser.ChatColor = chatColor;
            }

            if (apiUser != null)
            {
                if (twitchUser.UserLogin.StartsWith("user_"))
                {
                    twitchUser.UserLogin = apiUser.Login ?? twitchUser.UserLogin;
                }

                if (twitchUser.DisplayName.StartsWith("User"))
                {
                    twitchUser.DisplayName = apiUser.DisplayName ?? twitchUser.DisplayName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Не удалось обогатить данные из API для пользователя {TwitchId}",
                twitchUser.TwitchId
            );
        }
        return twitchUser;
    }
}
