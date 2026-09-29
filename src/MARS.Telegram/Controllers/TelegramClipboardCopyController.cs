using MARS.Shared.Models;
using MARS.Telegram.Services;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Telegram.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramClipboardCopyController(
    ITelegramClipboardCopyService telegramClipboardCopyService
) : ControllerBase
{
    [HttpGet("{requestId}")]
    public async Task<ActionResult<OperationResult<string[]>>> GetFilesByRequestId(string requestId)
    {
        var result = OperationResult<string[]>.Fail("ID запроса не передан");

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            result = await telegramClipboardCopyService.GetFileUrlsByRequestIdAsync(requestId);
        }

        return Ok(result);
    }

    [HttpPost("complete/{requestId}")]
    public async Task<ActionResult<OperationResult>> CompleteRequest(string requestId)
    {
        var result = await telegramClipboardCopyService.MarkRequestAsCompletedAsync(requestId);
        return Ok(result);
    }
}
