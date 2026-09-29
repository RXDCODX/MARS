using MARS.Shared.Models;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Services.AutoMessages;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutoMessagesController(
    IAutoMessagesService autoMessagesService,
    ILogger<AutoMessagesController> logger
) : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<OperationResult<IEnumerable<AutoMessageDto>>>
    > GetAllAutoMessages(CancellationToken cancellationToken = default)
    {
        ActionResult<OperationResult<IEnumerable<AutoMessageDto>>> result;
        try
        {
            var messages = await autoMessagesService.GetAllAutoMessagesAsync(cancellationToken);
            result = Ok(OperationResult<IEnumerable<AutoMessageDto>>.Ok(messages));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении всех автоматических сообщений");
            result = Ok(
                OperationResult<IEnumerable<AutoMessageDto>>.Fail(
                    "Ошибка при получении автоматических сообщений"
                )
            );
        }

        return result;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperationResult<AutoMessageDto?>>> GetAutoMessage(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<AutoMessageDto?>> result;
        try
        {
            var message = await autoMessagesService.GetAutoMessageByIdAsync(id, cancellationToken);

            if (message != null)
            {
                result = Ok(OperationResult<AutoMessageDto?>.Ok(message));
            }
            else
            {
                result = Ok(
                    OperationResult<AutoMessageDto?>.Fail(
                        $"Автоматическое сообщение с ID {id} не найдено"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении автоматического сообщения с ID: {Id}", id);
            result = Ok(
                OperationResult<AutoMessageDto?>.Fail(
                    "Ошибка при получении автоматического сообщения"
                )
            );
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<AutoMessageDto?>>> CreateAutoMessage(
        CreateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<AutoMessageDto?>> result;
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                result = Ok(
                    OperationResult<AutoMessageDto?>.Fail("Текст сообщения не может быть пустым")
                );
            }
            else
            {
                var message = await autoMessagesService.CreateAutoMessageAsync(
                    request,
                    cancellationToken
                );

                if (message.Id != Guid.Empty)
                {
                    result = Ok(OperationResult<AutoMessageDto?>.Ok(message));
                }
                else
                {
                    result = Ok(
                        OperationResult<AutoMessageDto?>.Fail(
                            "Не удалось создать автоматическое сообщение"
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании автоматического сообщения");
            result = Ok(
                OperationResult<AutoMessageDto?>.Fail(
                    "Ошибка при создании автоматического сообщения"
                )
            );
        }

        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OperationResult<AutoMessageDto?>>> UpdateAutoMessage(
        Guid id,
        UpdateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<AutoMessageDto?>> result;
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                result = Ok(
                    OperationResult<AutoMessageDto?>.Fail("Текст сообщения не может быть пустым")
                );
            }
            else
            {
                var message = await autoMessagesService.UpdateAutoMessageAsync(
                    id,
                    request,
                    cancellationToken
                );

                if (message != null)
                {
                    result = Ok(OperationResult<AutoMessageDto?>.Ok(message));
                }
                else
                {
                    result = Ok(
                        OperationResult<AutoMessageDto?>.Fail(
                            $"Автоматическое сообщение с ID {id} не найдено"
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении автоматического сообщения с ID: {Id}", id);
            result = Ok(
                OperationResult<AutoMessageDto?>.Fail(
                    "Ошибка при обновлении автоматического сообщения"
                )
            );
        }

        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<OperationResult>> DeleteAutoMessage(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult> result;
        try
        {
            var deleted = await autoMessagesService.DeleteAutoMessageAsync(id, cancellationToken);

            if (deleted)
            {
                result = Ok(OperationResult.Ok());
            }
            else
            {
                result = Ok(
                    OperationResult.Fail($"Автоматическое сообщение с ID {id} не найдено")
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении автоматического сообщения с ID: {Id}", id);
            result = Ok(OperationResult.Fail("Ошибка при удалении автоматического сообщения"));
        }

        return result;
    }
}
