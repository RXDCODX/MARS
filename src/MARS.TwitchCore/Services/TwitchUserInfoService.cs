using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;

namespace MARS.TwitchCore.Services;

public class TwitchUserInfoService(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<TwitchUserInfoService> logger
)
{
    public async Task<User?> GetUserInfoAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var token = await tokenService.GetValidTokenAsync(ct);

            if (token == null)
            {
                return null;
            }

            var response = await api.Helix.Users.GetUsersAsync(ids: [userId], accessToken: token.AccessToken);

            return response.Users.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении информации о пользователе {UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<string, User>> GetUsersInfoAsync(
        ICollection<string> userIds,
        CancellationToken ct = default
    )
    {
        var result = new Dictionary<string, User>();

        if (userIds.Count == 0)
        {
            return result;
        }

        var userIdsList = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

        if (userIdsList.Count == 0)
        {
            return result;
        }

        try
        {
            var token = await tokenService.GetValidTokenAsync(ct);

            if (token == null)
            {
                return result;
            }

            const int batchSize = 100;

            for (var i = 0; i < userIdsList.Count; i += batchSize)
            {
                var batch = userIdsList.Skip(i).Take(batchSize).ToList();
                var response = await api.Helix.Users.GetUsersAsync(ids: [.. batch], accessToken: token.AccessToken);

                foreach (var user in response.Users)
                {
                    result[user.Id] = user;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении информации о пользователях");
        }

        return result;
    }

    public async Task<string?> GetUserChatColorAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var token = await tokenService.GetValidTokenAsync(ct);

            if (token == null)
            {
                return null;
            }

            var response = await api.Helix.Chat.GetUserChatColorAsync(
                userIds: [userId],
                accessToken: token.AccessToken
            );

            return response.Data.FirstOrDefault()?.Color;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении цвета чата пользователя {UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<string, string?>> GetUsersChatColorsAsync(
        IEnumerable<string> userIds,
        CancellationToken ct = default
    )
    {
        var result = new Dictionary<string, string?>();

        IEnumerable<string> enumerable = userIds as string[] ?? [.. userIds];
        if (!enumerable.Any())
        {
            return result;
        }

        var userIdsList = enumerable.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

        if (userIdsList.Count == 0)
        {
            return result;
        }

        try
        {
            var token = await tokenService.GetValidTokenAsync(ct);

            if (token == null)
            {
                return result;
            }

            const int batchSize = 100;

            for (var i = 0; i < userIdsList.Count; i += batchSize)
            {
                var batch = userIdsList.Skip(i).Take(batchSize);
                var response = await api.Helix.Chat.GetUserChatColorAsync(
                    userIds: [.. batch],
                    accessToken: token.AccessToken
                );

                foreach (var userColor in response.Data)
                {
                    result[userColor.UserId] = userColor.Color;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении цветов чата пользователей");
        }

        return result;
    }

    public List<string> GetUsersWithoutAvatars(ICollection<FollowerInfo> followersInfo)
    {
        return followersInfo.Count == 0
            ? []
            :
            [
                .. followersInfo
                    .Where(f =>
                        f.TwitchUser == null
                        || string.IsNullOrWhiteSpace(f.TwitchUser.ProfileImageUrl)
                    )
                    .Select(f => f.UserId),
            ];
    }
}
