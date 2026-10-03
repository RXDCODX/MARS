using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Extensions;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Commands.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController(
    ICommandService commandService,
    ApiCommandService apiCommandService,
    ILogger<CommandsController> logger
) : ControllerBase
{
    [HttpGet("user")]
    public ActionResult<OperationResult<string[]>> GetUserCommands(
        CancellationToken cancellationToken = default
    )
    {
        var commands = commandService.GetUserCommands(true, cancellationToken);
        return Ok(OperationResult<string[]>.Ok(commands));
    }

    [HttpGet("admin")]
    public ActionResult<OperationResult<string[]>> GetAdminCommands(
        CancellationToken cancellationToken = default
    )
    {
        var commands = commandService.GetAdminCommands(true, cancellationToken);
        return Ok(OperationResult<string[]>.Ok(commands));
    }

    [HttpGet("user/{platform}")]
    public ActionResult<OperationResult<string[]>> GetUserCommandsByPlatform(
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var commands = commandService.GetUserCommands(platform, true, cancellationToken);
        return Ok(OperationResult<string[]>.Ok(commands));
    }

    [HttpGet("admin/{platform}")]
    public ActionResult<OperationResult<string[]>> GetAdminCommandsByPlatform(
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var commands = commandService.GetAdminCommands(platform, true, cancellationToken);
        return Ok(OperationResult<string[]>.Ok(commands));
    }

    /// <summary>
    /// Пользовательские команды вместе с описанием.
    /// </summary>
    /// <remarks>
    /// Маршрут нужен клиенту, а не «на всякий случай»: страница команд и экран
    /// очереди звуковых запросов зовут именно его, потому что форма пути взята
    /// сгенерированным клиентом из монолита. Без маршрута обе страницы получали
    /// 404 и были пустыми на живом стенде.
    /// <para>
    /// Данные берутся из <see cref="ApiCommandService"/>, где та же выборка по
    /// платформе уже есть: править клиент под более короткий путь значило бы
    /// чинить отсутствующий маршрут переименованием на стороне сервиса.
    /// </para>
    /// </remarks>
    [HttpGet("user/platform/{platform}/info")]
    public ActionResult<OperationResult<BaseCommand[]>> GetUserCommandsInfoByPlatform(
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var commands = apiCommandService.GetUserCommandsInfo(platform);
        return Ok(OperationResult<BaseCommand[]>.Ok(commands));
    }

    /// <summary>
    /// Административные команды вместе с описанием.
    /// </summary>
    /// <inheritdoc cref="GetUserCommandsInfoByPlatform" path="/remarks" />
    [HttpGet("admin/platform/{platform}/info")]
    public ActionResult<OperationResult<BaseCommand[]>> GetAdminCommandsInfoByPlatform(
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var commands = apiCommandService.GetAdminCommandsInfo(platform);
        return Ok(OperationResult<BaseCommand[]>.Ok(commands));
    }

    [HttpGet("{commandName}/parameters")]
    public ActionResult<OperationResult<CommandParameterInfo[]>> GetCommandParameters(
        string commandName,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = commandService.GetCommandParameters(commandName, cancellationToken);
        if (parameters is null)
        {
            return Ok(
                OperationResult<CommandParameterInfo[]>.Fail($"Команда '{commandName}' не найдена.")
            );
        }

        return Ok(OperationResult<CommandParameterInfo[]>.Ok(parameters));
    }

    /// <summary>
    /// Выполнение команды закрыто ключом межсервисной аутентификации: маршрут
    /// <c>/api/Commands/{**remainder}</c> выведен через YARP наружу, и без
    /// защиты любой, кто достал до шлюза, запускал бы в том числе <c>shutdown</c>
    /// и <c>setenv</c>. Списки команд и параметров остаются открытыми — их
    /// читает Swagger-агрегатор и админский UI без ключа.
    /// </summary>
    /// <summary>
    /// Возвращает структуру, а не строку: код ошибки должен быть виден вызывающему.
    /// Бизнес-ошибка команды — это <c>Ok</c> с <c>Success = false</c> внутри
    /// результата, потому что HTTP-запрос был обработан.
    /// </summary>
    [Authorize(Policy = ServiceAuthExtensions.PolicyName)]
    [HttpPost("{commandName}/execute")]
    public async Task<ActionResult<OperationResult<CommandResult>>> ExecuteCommand(
        string commandName,
        [FromBody] string input,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<CommandResult>> result;

        try
        {
            var response = await apiCommandService.ExecuteCommandAsync(
                commandName,
                input,
                cancellationToken
            );

            result = response.Success
                ? Ok(OperationResult<CommandResult>.Ok(response))
                : Ok(OperationResult<CommandResult>.Fail(response.Text));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {CommandName}", commandName);
            result = Ok(
                OperationResult<CommandResult>.Fail($"Error executing command: {ex.Message}")
            );
        }

        return result;
    }
}
