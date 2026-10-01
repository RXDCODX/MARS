using MARS.Shared.Models;
using MARS.TTS.Services;

namespace MARS.TTS.Tests.Grpc;

/// <summary>
/// Заглушка фильтра TTS: по умолчанию пропускает сообщение как есть, а тест
/// может включить фильтр и запретить конкретный текст.
/// </summary>
public sealed class FakeTtsMessageFilterService : ITtsMessageFilterService
{
    public bool IsFilterEnabled { get; set; }

    public string? RejectedText { get; set; }

    public OperationResult<string> FilterMessage(string message, string? userId = null)
    {
        if (message == RejectedText)
        {
            return OperationResult<string>.Fail("сообщение заблокировано");
        }

        return OperationResult<string>.Ok(message);
    }

    public Task LoadStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
