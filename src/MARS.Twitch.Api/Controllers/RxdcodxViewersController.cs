using MARS.Shared.Kernel;
using MARS.Twitch.Api.Entities;
using MARS.Twitch.Api.Services.TwitchFollowers;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Twitch.Api.Controllers;

/// <summary>
/// Информация о зрителях канала rxdcodx.
/// </summary>
/// <remarks>
/// Данные живут в базе Twitch-сервиса (<c>twitch.FollowerInfos</c> и
/// <c>twitch.TwitchUsers</c>), поэтому эндпоинт обслуживается здесь, а не в
/// MARS.Admin: там была только заглушка сущности без реализации сервиса.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class RxdcodxViewersController(
    IRxdcodxViewersService viewersService,
    ILogger<RxdcodxViewersController> logger
) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<OperationResult<List<FollowerInfo>?>>> GetAll(
        [FromQuery] bool forceUseCash
    )
    {
        ActionResult<OperationResult<List<FollowerInfo>?>> result;
        try
        {
            var allUsers = await viewersService.GetAllFollowersInfo(forceUseCash);

            result = Ok(
                allUsers != null
                    ? OperationResult<List<FollowerInfo>?>.Ok(allUsers)
                    : OperationResult<List<FollowerInfo>?>.Fail("Токен недоступен")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении всех пользователей");
            result = Ok(
                OperationResult<List<FollowerInfo>?>.Fail("Ошибка при получении пользователей")
            );
        }

        return result;
    }

    /// <summary>
    /// Получить пользователей без аватарок
    /// </summary>
    [HttpGet("without-avatars")]
    public async Task<ActionResult<OperationResult<List<FollowerInfo>>>> GetUsersWithoutAvatars()
    {
        ActionResult<OperationResult<List<FollowerInfo>>> result;
        try
        {
            var usersWithoutAvatars = await viewersService.GetUsersWithoutAvatarsAsync();
            result = Ok(OperationResult<List<FollowerInfo>>.Ok(usersWithoutAvatars));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении пользователей без аватарок");
            result = Ok(
                OperationResult<List<FollowerInfo>>.Fail(
                    "Ошибка при получении пользователей без аватарок"
                )
            );
        }

        return result;
    }

    /// <summary>
    /// Получить количество пользователей без аватарок
    /// </summary>
    [HttpGet("without-avatars/count")]
    public async Task<ActionResult<OperationResult<int>>> GetUsersWithoutAvatarsCount()
    {
        ActionResult<OperationResult<int>> result;
        try
        {
            var count = await viewersService.GetUsersWithoutAvatarsCountAsync();
            result = Ok(OperationResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при подсчете пользователей без аватарок");
            result = Ok(
                OperationResult<int>.Fail("Ошибка при подсчете пользователей без аватарок")
            );
        }

        return result;
    }

    /// <summary>
    /// Обновить аватарки для пользователей без них
    /// </summary>
    [HttpPost("update-avatars")]
    public async Task<ActionResult<OperationResult<int>>> UpdateMissingAvatars()
    {
        ActionResult<OperationResult<int>> result;
        try
        {
            var updatedCount = await viewersService.UpdateMissingAvatarsAsync();
            result = Ok(OperationResult<int>.Ok(updatedCount));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении аватарок");
            result = Ok(OperationResult<int>.Fail("Ошибка при обновлении аватарок"));
        }

        return result;
    }

    /// <summary>
    /// Тестовый endpoint для проверки обновления аватарок
    /// </summary>
    [HttpGet("debug/avatars")]
    public async Task<ActionResult<OperationResult<RxdcodxAvatarsDebugDto>>> DebugAvatars()
    {
        ActionResult<OperationResult<RxdcodxAvatarsDebugDto>> result;

        try
        {
            var usersWithoutAvatars = await viewersService.GetUsersWithoutAvatarsAsync();
            var count = await viewersService.GetUsersWithoutAvatarsCountAsync();

            var debugInfo = new RxdcodxAvatarsDebugDto
            {
                UsersWithoutAvatarsCount = count,
                UsersWithoutAvatars = usersWithoutAvatars
                    .Take(5)
                    .Select(u => new RxdcodxAvatarMissingUserDto
                    {
                        UserId = u.UserId,
                        UserName = u.TwitchUser?.DisplayName,
                        ProfileImageUrl = u.TwitchUser?.ProfileImageUrl,
                        LastUpdated = u.TwitchUser?.LastUpdated,
                    })
                    .ToList(),
                Message = $"Найдено {count} пользователей без аватарок",
            };

            result = Ok(OperationResult<RxdcodxAvatarsDebugDto>.Ok(debugInfo));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении отладочной информации об аватарках");
            result = Ok(
                OperationResult<RxdcodxAvatarsDebugDto>.Fail(
                    "Ошибка при получении отладочной информации"
                )
            );
        }

        return result;
    }
}
