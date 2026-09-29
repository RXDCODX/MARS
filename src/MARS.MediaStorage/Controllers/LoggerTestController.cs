using MARS.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggerTestController(ILogger<LoggerTestController> logger) : ControllerBase
{
    [HttpPost("test-logging")]
    public ActionResult<OperationResult> TestLogging()
    {
        logger.LogTrace("Это сообщение уровня Trace - детальная отладочная информация");
        logger.LogDebug("Это сообщение уровня Debug - отладочная информация");
        logger.LogInformation("Это сообщение уровня Information - общая информация");
        logger.LogWarning("Это сообщение уровня Warning - предупреждение");
        logger.LogError("Это сообщение уровня Error - ошибка");
        logger.LogCritical("Это сообщение уровня Critical - критическая ошибка");

        ActionResult<OperationResult> result = Ok(OperationResult.Ok());

        return result;
    }

    [HttpPost("test-exception")]
    public ActionResult<OperationResult> TestException()
    {
        try
        {
            throw new InvalidOperationException(
                "Это тестовое исключение для демонстрации логирования"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла ошибка при выполнении тестового метода");
        }

        ActionResult<OperationResult> result = Ok(OperationResult.Ok());

        return result;
    }

    [HttpPost("test-structured")]
    public ActionResult<OperationResult> TestStructuredLogging()
    {
        var userId = "user123";
        var action = "test_action";
        var duration = 150;

        logger.LogInformation(
            "Пользователь {UserId} выполнил действие {Action} за {Duration}ms",
            userId,
            action,
            duration
        );

        logger.LogWarning(
            "Попытка доступа пользователя {UserId} к ресурсу {Resource} была отклонена",
            userId,
            "protected_resource"
        );

        ActionResult<OperationResult> result = Ok(OperationResult.Ok());

        return result;
    }
}
