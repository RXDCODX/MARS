using MARS.Shared.Models;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TwitchUsersController(
    IDbContextFactory<TwitchDbContext> dbFactory,
    ILogger<TwitchUsersController> logger
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<List<TwitchUserDto>>>> GetAllUsers(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<List<TwitchUserDto>>> result;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var users = await db
                .TwitchUsers.AsNoTracking()
                .OrderBy(u => u.DisplayName)
                .Select(u => new TwitchUserDto
                {
                    TwitchId = u.TwitchId,
                    UserLogin = u.UserLogin,
                    DisplayName = u.DisplayName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    ChatColor = u.ChatColor,
                    IsModerator = u.IsModerator,
                    IsVip = u.IsVip,
                    IsBroadcaster = u.TwitchId == TwitchConstants.ChannelId,
                    IsInBlockList = u.IsInBlockList,
                    AliasNickname = u.AliasNickname,
                    FollowedAt = u.FollowedAt,
                    LastUpdated = u.LastUpdated,
                    CreatedAt = u.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            result = Ok(OperationResult<List<TwitchUserDto>>.Ok(users));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<List<TwitchUserDto>>.Fail("Ошибка при получении пользователей Twitch")
            );
        }

        return result;
    }

    /// <summary>
    /// Поиск пользователя по логину. Команды <c>fumoinv</c> и <c>mikuinv</c>
    /// принимают имя пользователя, а инвентарь лежит в коллекции по Twitch ID,
    /// поэтому преобразование логин → ID живёт рядом с таблицей пользователей.
    /// </summary>
    /// <remarks>
    /// Пользователь, которого нет в базе, возвращается успехом с <c>null</c>,
    /// а не ошибкой: «такого игрока нет» — это ответ, а не сбой. В базу он не
    /// попадает, потому что создание пользователей — отдельная задача сервиса
    /// синхронизации.
    /// </remarks>
    [HttpGet("by-login/{login}")]
    public async Task<ActionResult<OperationResult<string?>>> GetUserIdByLogin(
        [FromRoute] string login,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string?>> result;

        try
        {
            var normalized = login.Trim().TrimStart('@').ToLowerInvariant();

            if (normalized.Length == 0)
            {
                result = Ok(OperationResult<string?>.Fail("Логин не передан"));
            }
            else
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

                var twitchId = await db
                    .TwitchUsers.AsNoTracking()
                    .Where(u => u.UserLogin == normalized)
                    .Select(u => u.TwitchId)
                    .FirstOrDefaultAsync(cancellationToken);

                result = Ok(OperationResult<string?>.Ok(string.IsNullOrEmpty(twitchId) ? null : twitchId));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при поиске пользователя по логину {Login}", login);
            result = Ok(OperationResult<string?>.Fail("Ошибка при поиске пользователя по логину"));
        }

        return result;
    }

    public async Task<ActionResult<OperationResult<TwitchUserDto?>>> GetUser(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<TwitchUserDto?>> result;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var user = await db
                .TwitchUsers.AsNoTracking()
                .Where(u => u.TwitchId == id)
                .Select(u => new TwitchUserDto
                {
                    TwitchId = u.TwitchId,
                    UserLogin = u.UserLogin,
                    DisplayName = u.DisplayName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    ChatColor = u.ChatColor,
                    IsModerator = u.IsModerator,
                    IsVip = u.IsVip,
                    IsBroadcaster = u.TwitchId == TwitchConstants.ChannelId,
                    IsInBlockList = u.IsInBlockList,
                    AliasNickname = u.AliasNickname,
                    FollowedAt = u.FollowedAt,
                    LastUpdated = u.LastUpdated,
                    CreatedAt = u.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user != null)
            {
                result = Ok(OperationResult<TwitchUserDto?>.Ok(user));
            }
            else
            {
                result = Ok(
                    OperationResult<TwitchUserDto?>.Fail($"Пользователь с ID {id} не найден")
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<TwitchUserDto?>.Fail("Ошибка при получении пользователя"));
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<TwitchUserDto?>>> CreateUser(
        CreateTwitchUserRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<TwitchUserDto?>> result;
        try
        {
            if (
                request == null
                || string.IsNullOrWhiteSpace(request.TwitchId)
                || string.IsNullOrWhiteSpace(request.UserLogin)
                || string.IsNullOrWhiteSpace(request.DisplayName)
            )
            {
                result = Ok(
                    OperationResult<TwitchUserDto?>.Fail(
                        "TwitchId, UserLogin и DisplayName обязательны"
                    )
                );
                return result;
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var exists = await db
                .TwitchUsers.AsNoTracking()
                .AnyAsync(u => u.TwitchId == request.TwitchId, cancellationToken);

            if (exists)
            {
                result = Ok(
                    OperationResult<TwitchUserDto?>.Fail(
                        $"Пользователь с ID {request.TwitchId} уже существует"
                    )
                );
                return result;
            }

            var user = new MARS.TwitchCore.Entities.TwitchUser
            {
                TwitchId = request.TwitchId,
                UserLogin = request.UserLogin,
                DisplayName = request.DisplayName,
                ProfileImageUrl = request.ProfileImageUrl,
                ChatColor = request.ChatColor,
                IsModerator = request.IsModerator,
                IsVip = request.IsVip,
                IsInBlockList = request.IsInBlockList,
                AliasNickname = request.AliasNickname,
                CreatedAt = DateTime.Now,
                LastUpdated = DateTime.Now,
            };

            db.TwitchUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            var dto = new TwitchUserDto
            {
                TwitchId = user.TwitchId,
                UserLogin = user.UserLogin,
                DisplayName = user.DisplayName,
                ProfileImageUrl = user.ProfileImageUrl,
                ChatColor = user.ChatColor,
                IsModerator = user.IsModerator,
                IsVip = user.IsVip,
                IsBroadcaster = user.TwitchId == TwitchConstants.ChannelId,
                IsInBlockList = user.IsInBlockList,
                AliasNickname = user.AliasNickname,
                FollowedAt = user.FollowedAt,
                LastUpdated = user.LastUpdated,
                CreatedAt = user.CreatedAt,
            };

            result = Ok(OperationResult<TwitchUserDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<TwitchUserDto?>.Fail("Ошибка при создании пользователя"));
        }

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OperationResult<TwitchUserDto?>>> UpdateUser(
        string id,
        UpdateTwitchUserRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<TwitchUserDto?>> result;
        try
        {
            if (request == null)
            {
                result = Ok(
                    OperationResult<TwitchUserDto?>.Fail("Тело запроса не может быть пустым")
                );
                return result;
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.TwitchUsers.FirstOrDefaultAsync(
                u => u.TwitchId == id,
                cancellationToken
            );

            if (user == null)
            {
                result = Ok(
                    OperationResult<TwitchUserDto?>.Fail($"Пользователь с ID {id} не найден")
                );
                return result;
            }

            if (request.UserLogin != null)
                user.UserLogin = request.UserLogin;
            if (request.DisplayName != null)
                user.DisplayName = request.DisplayName;
            if (request.ProfileImageUrl != null)
                user.ProfileImageUrl = request.ProfileImageUrl;
            if (request.ChatColor != null)
                user.ChatColor = request.ChatColor;
            if (request.IsModerator.HasValue)
                user.IsModerator = request.IsModerator.Value;
            if (request.IsVip.HasValue)
                user.IsVip = request.IsVip.Value;
            if (request.IsInBlockList.HasValue)
                user.IsInBlockList = request.IsInBlockList.Value;
            if (request.AliasNickname != null)
                user.AliasNickname = request.AliasNickname;

            user.LastUpdated = DateTime.Now;
            await db.SaveChangesAsync(cancellationToken);

            var dto = new TwitchUserDto
            {
                TwitchId = user.TwitchId,
                UserLogin = user.UserLogin,
                DisplayName = user.DisplayName,
                ProfileImageUrl = user.ProfileImageUrl,
                ChatColor = user.ChatColor,
                IsModerator = user.IsModerator,
                IsVip = user.IsVip,
                IsBroadcaster = user.TwitchId == TwitchConstants.ChannelId,
                IsInBlockList = user.IsInBlockList,
                AliasNickname = user.AliasNickname,
                FollowedAt = user.FollowedAt,
                LastUpdated = user.LastUpdated,
                CreatedAt = user.CreatedAt,
            };

            result = Ok(OperationResult<TwitchUserDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<TwitchUserDto?>.Fail("Ошибка при обновлении пользователя"));
        }

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<OperationResult>> DeleteUser(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult> result;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.TwitchUsers.FirstOrDefaultAsync(
                u => u.TwitchId == id,
                cancellationToken
            );

            if (user == null)
            {
                result = Ok(OperationResult.Fail($"Пользователь с ID {id} не найден"));
                return result;
            }

            db.TwitchUsers.Remove(user);
            await db.SaveChangesAsync(cancellationToken);

            result = Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail("Ошибка при удалении пользователя"));
        }

        return result;
    }
}
