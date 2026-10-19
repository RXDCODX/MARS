using System.Collections.Concurrent;
using MARS.Shared.Models;
using MARS.Telegram.Models;
using MARS.Telegram.Services.BotService;
using MARS.Telegram.Services.MemoryStorageService;
using MARS.Telegram.Services.Messaging;
using TL;

namespace MARS.Telegram.Services;

public class TelegramClipboardCopyService(
    ILogger<TelegramClipboardCopyService> logger,
    IHostApplicationLifetime applicationLifetime,
    IConfiguration configuration
) : ITelegramClipboardCopyService, ITelegramIncomingHandler
{
    private const int MediaGroupDebounceMs = 1200;
    private const int TriggerTimeoutMs = 1000;
    private const string ClipboardPagePath = "/telegram-copy";
    private const int DefaultRequestTtlMinutes = 30;

    private static readonly string[] TriggerWords = ["copy", "копи", "копипаста", "копировать"];
    private readonly ConcurrentDictionary<long, MediaGroupBuffer> _mediaGroupBuffers = new();
    private readonly ConcurrentDictionary<long, TriggerWaitBuffer> _triggerWaitBuffers = new();
    private readonly ConcurrentDictionary<string, ClipboardRequestFiles> _clipboardRequests = new();
    private readonly TimeSpan _requestTtl = ResolveRequestTtl(configuration);

    private CancellationToken StoppingToken => applicationLifetime.ApplicationStopping;

    /// <inheritdoc />
    public Task HandleAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        CancellationToken cancellationToken
    )
    {
        var result = HandleMessageAsync(client, message);

        return result;
    }

    private async Task HandleMessageAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message
    )
    {
        var chatId = message.ChatId;
        var textWithTrigger = message.Text;
        var hasTrigger = ContainsTrigger(textWithTrigger);
        var hasPhoto = message.Photo is not null;

        logger.LogDebug(
            "HandleMessage: chatId={ChatId}, hasTrigger={HasTrigger}, hasPhoto={HasPhoto}, mediaGroupId={MediaGroupId}",
            chatId,
            hasTrigger,
            hasPhoto,
            message.MediaGroupId?.ToString() ?? "null"
        );

        if (!hasPhoto)
        {
            if (hasTrigger)
            {
                await HandleTriggerMessageAsync(chatId);
            }
            return;
        }

        var triggerActive = await CheckAndClearTriggerAsync(chatId);
        var shouldProcess = triggerActive || hasTrigger;

        logger.LogDebug(
            "HandleMessage photo: chatId={ChatId}, triggerActive={TriggerActive}, hasTrigger={HasTrigger}, shouldProcess={ShouldProcess}",
            chatId,
            triggerActive,
            hasTrigger,
            shouldProcess
        );

        if (message.MediaGroupId is not { } mediaGroupId)
        {
            if (shouldProcess)
            {
                logger.LogInformation("Processing single photo: chatId={ChatId}", chatId);
                await ProcessMessagesAsync(client, message, [message], null);
            }
        }
        else
        {
            if (shouldProcess)
            {
                logger.LogInformation(
                    "Processing media group: chatId={ChatId}, mediaGroupId={MediaGroupId}",
                    chatId,
                    mediaGroupId
                );
            }
            await HandleMediaGroupMessageAsync(client, message, shouldProcess);
        }
    }

    private async Task HandleMediaGroupMessageAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        bool hasTrigger
    )
    {
        var mediaGroupId = message.MediaGroupId!.Value;
        var stateExists = _mediaGroupBuffers.TryGetValue(mediaGroupId, out var existingState);

        if (!stateExists && !hasTrigger)
        {
            return;
        }

        var state = existingState;
        if (!stateExists)
        {
            state = new MediaGroupBuffer();
            _mediaGroupBuffers[mediaGroupId] = state;
        }

        state!.Messages[message.MessageId] = message;
        state.ResetDebounce();

        var debounceCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken);
        state.DebounceCts = debounceCts;

        try
        {
            await Task.Delay(MediaGroupDebounceMs, debounceCts.Token);

            if (Interlocked.Exchange(ref state.IsProcessed, 1) == 0)
            {
                _mediaGroupBuffers.TryRemove(mediaGroupId, out _);
                var orderedMessages = state.Messages.Values.OrderBy(m => m.MessageId).ToList();
                await ProcessMessagesAsync(client, message, orderedMessages, mediaGroupId);
            }
        }
        catch (TaskCanceledException)
        {
            // Следующее сообщение в альбоме сбросило таймер.
        }
    }

    private async Task HandleTriggerMessageAsync(long chatId)
    {
        var triggerBuffer = new TriggerWaitBuffer { HasTrigger = true };
        _triggerWaitBuffers[chatId] = triggerBuffer;

        logger.LogInformation(
            "Trigger set: chatId={ChatId}, waiting for media group for {TriggerTimeoutMs}ms",
            chatId,
            TriggerTimeoutMs
        );

        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken);
        triggerBuffer.TimeoutCts = timeoutCts;

        try
        {
            await Task.Delay(TriggerTimeoutMs, timeoutCts.Token);
            _triggerWaitBuffers.TryRemove(chatId, out _);
            logger.LogInformation(
                "Trigger timeout: chatId={ChatId}, no media group received",
                chatId
            );
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation(
                "Trigger activated: chatId={ChatId}, media group received in time",
                chatId
            );
        }
    }

    private async Task<bool> CheckAndClearTriggerAsync(long chatId)
    {
        var result = false;

        if (_triggerWaitBuffers.TryRemove(chatId, out var triggerBuffer))
        {
            result = triggerBuffer.HasTrigger;
            await triggerBuffer.TimeoutCts?.CancelAsync()!;
            triggerBuffer.TimeoutCts?.Dispose();
            logger.LogInformation(
                "Trigger checked: chatId={ChatId}, hasTrigger={HasTrigger}",
                chatId,
                result
            );
        }

        return result;
    }

    private async Task ProcessMessagesAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage source,
        IReadOnlyCollection<TelegramIncomingMessage> messages,
        long? mediaGroupId
    )
    {
        await CleanupExpiredRequestsAsync();

        var memoryFileNames = new List<string>();
        var fileIndex = 1;
        var requestId = BuildRequestId(mediaGroupId);

        logger.LogInformation(
            "ProcessMessages started: chatId={ChatId}, messageCount={MessageCount}, mediaGroupId={MediaGroupId}, requestId={RequestId}",
            source.ChatId,
            messages.Count,
            mediaGroupId?.ToString() ?? "null",
            requestId
        );

        foreach (var message in messages)
        {
            try
            {
                var photo = message.Photo;
                var size = LargestSize(photo);

                if (photo is null || size is null)
                {
                    continue;
                }

                var memoryFileName = BuildMemoryFileName(requestId, fileIndex, ".jpg");

                await using var memoryStream = new MemoryStream();
                await client.DownloadFileAsync(photo, memoryStream, size, StoppingToken);

                var bytes = memoryStream.ToArray();
                await MemoryStorage.AddFileAsync(memoryFileName, bytes);

                memoryFileNames.Add(memoryFileName);
                logger.LogDebug(
                    "File downloaded: {FileName}, size={FileSize}",
                    memoryFileName,
                    bytes.Length
                );
                fileIndex += 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка загрузки фото для буфера обмена");
            }
        }

        logger.LogInformation(
            "Files prepared: requestId={RequestId}, count={FileCount}",
            requestId,
            memoryFileNames.Count
        );

        var answerText = BuildResultMessage(memoryFileNames, requestId);

        logger.LogInformation(
            "Request registered: requestId={RequestId}, savedCount={SavedCount}",
            requestId,
            _clipboardRequests.ContainsKey(requestId) ? memoryFileNames.Count : 0
        );

        try
        {
            await SendHtmlAsync(client, source.Peer, answerText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отправки сообщения с URL буфера обмена");
        }
    }

    /// <summary>
    /// Наибольший размер фото: Telegram присылает несколько, и брать первый
    /// означало бы отдать копию, годящую разве что для аватарки.
    /// </summary>
    private static PhotoSize? LargestSize(TL.Photo? photo)
    {
        PhotoSize? result = null;

        foreach (var size in photo?.sizes ?? [])
        {
            if (
                size is PhotoSize { } known
                && (result is not PhotoSize current || known.size > current.size)
            )
            {
                result = known;
            }
        }

        return result;
    }

    /// <summary>
    /// Ответ с разметкой. У Bot API теги разбирал Telegram по
    /// <c>ParseMode.Html</c>, теперь разбор наш — см. <see cref="HtmlMessageEntities"/>.
    /// </summary>
    private static Task SendHtmlAsync(ITelegramBotConnection client, InputPeer peer, string html)
    {
        var text = HtmlMessageEntities.Parse(html, out var entities);

        return client.SendTextAsync(peer, text, entities);
    }

    public async Task<OperationResult<string[]>> GetFileUrlsByRequestIdAsync(string requestId)
    {
        var result = OperationResult<string[]>.Fail("Запрос не найден или файлы уже недоступны");

        await CleanupExpiredRequestsAsync();

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var found = _clipboardRequests.TryGetValue(requestId, out var files);

            if (found && files is not null && files.MemoryFileNames.Length > 0)
            {
                if (!IsExpired(files.CreatedAt))
                {
                    var urls = files.MemoryFileNames.Select(ToMemoryUrl).ToArray();
                    result = OperationResult<string[]>.Ok(urls);
                    logger.LogInformation(
                        "GetFileUrls: SUCCESS - requestId={RequestId}, fileCount={FileCount}",
                        requestId,
                        files.MemoryFileNames.Length
                    );
                }
                else
                {
                    _clipboardRequests.TryRemove(requestId, out _);
                    await CleanupMemoryFilesAsync(files.MemoryFileNames);
                    result = OperationResult<string[]>.Fail("Срок действия запроса истек");
                    logger.LogWarning(
                        "GetFileUrls: REQUEST_EXPIRED - requestId={RequestId}, age={AgeSec}s",
                        requestId,
                        (DateTime.Now - files.CreatedAt).TotalSeconds
                    );
                }
            }
            else
            {
                logger.LogDebug(
                    "GetFileUrls: NOT_FOUND - requestId={RequestId}, found={Found}, fileCount={FileCount}, totalRequests={TotalRequests}",
                    requestId,
                    found,
                    files?.MemoryFileNames.Length ?? 0,
                    _clipboardRequests.Count
                );
            }
        }

        return result;
    }

    public async Task<OperationResult> MarkRequestAsCompletedAsync(string requestId)
    {
        var result = OperationResult.Fail("ID запроса не передан");

        await CleanupExpiredRequestsAsync();

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var removed = _clipboardRequests.TryRemove(requestId, out var files);

            if (removed)
            {
                if (files is not null)
                {
                    await CleanupMemoryFilesAsync(files.MemoryFileNames);
                }
                result = OperationResult.Ok();
            }
            else
            {
                result = OperationResult.Fail("Запрос не найден");
            }
        }

        return result;
    }

    private string BuildResultMessage(IReadOnlyCollection<string> memoryFileNames, string requestId)
    {
        var result = "❌ Не удалось скачать изображения для копирования.";

        if (memoryFileNames.Count > 0)
        {
            var requestFiles = new ClipboardRequestFiles(memoryFileNames.ToArray(), DateTime.Now);
            _clipboardRequests[requestId] = requestFiles;

            logger.LogInformation(
                "BuildResultMessage: SUCCESS - requestId={RequestId}, fileCount={FileCount}, saved={Saved}",
                requestId,
                memoryFileNames.Count,
                _clipboardRequests.ContainsKey(requestId)
            );

            var pageUrl = BuildClipboardPageUrl(requestId);
            var escapedUrl = pageUrl
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            result =
                $"✅ <b>Готово!</b> Файлов загружено: <b>{memoryFileNames.Count}</b>\n\n"
                + $"Откройте ссылку и нажмите кнопку «Скопировать все»:\n"
                + $"<a href=\"{escapedUrl}\">Открыть страницу</a>\n"
                + $"<code>{escapedUrl}</code>";
        }
        else
        {
            logger.LogWarning(
                "BuildResultMessage: EMPTY - requestId={RequestId}, noFiles",
                requestId
            );
        }

        return result;
    }

    private async Task CleanupExpiredRequestsAsync()
    {
        var snapshot = _clipboardRequests.ToArray();

        foreach (var item in snapshot)
        {
            if (IsExpired(item.Value.CreatedAt))
            {
                var removed = _clipboardRequests.TryRemove(item.Key, out var removedFiles);
                if (removed && removedFiles is not null)
                {
                    logger.LogInformation(
                        "Cleanup: removing requestId={RequestId}, fileCount={FileCount}, age={AgeSec}s",
                        item.Key,
                        removedFiles.MemoryFileNames.Length,
                        (DateTime.Now - removedFiles.CreatedAt).TotalSeconds
                    );
                    await CleanupMemoryFilesAsync(removedFiles.MemoryFileNames);
                }
            }
        }
    }

    private static TimeSpan ResolveRequestTtl(IConfiguration configurationValue)
    {
        var configuredMinutes =
            configurationValue["AppSettings:TelegramClipboardCopy:RequestTtlMinutes"]
            ?? configurationValue["TelegramClipboardCopy:RequestTtlMinutes"];

        var parsed = int.TryParse(configuredMinutes, out var minutes);
        if (!parsed || minutes <= 0)
        {
            minutes = DefaultRequestTtlMinutes;
        }

        return TimeSpan.FromMinutes(minutes);
    }

    private async Task CleanupMemoryFilesAsync(IReadOnlyCollection<string> memoryFileNames)
    {
        foreach (var memoryFileName in memoryFileNames)
        {
            try
            {
                if (MemoryStorage.FileExists(memoryFileName))
                {
                    await MemoryStorage.DeleteFileAsync(memoryFileName);
                    logger.LogDebug("Memory file deleted: {FileName}", memoryFileName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось удалить файл {FileName} при очистке",
                    memoryFileName
                );
            }
        }
    }

    private bool IsExpired(DateTimeOffset createdAt)
    {
        var result = DateTime.Now - createdAt > _requestTtl;
        return result;
    }

    private static bool ContainsTrigger(string sourceText)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            var normalized = sourceText.ToLowerInvariant();
            result = TriggerWords.Any(word => normalized.Contains(word, StringComparison.Ordinal));
        }

        return result;
    }

    private string BuildRequestId(long? mediaGroupId)
    {
        // Альбом из нескольких фото должен уезжать по одной ссылке, иначе
        // администратору придёт столько страниц, сколько он отправил фотографий.
        var result = mediaGroupId?.ToString();

        if (string.IsNullOrWhiteSpace(result))
        {
            result = Guid.NewGuid().ToString("N");
        }

        return result;
    }

    private static string BuildMemoryFileName(string requestId, int fileIndex, string extension)
    {
        var result = $"telegram-copy/{requestId}/{fileIndex:D2}{extension}";

        return result;
    }

    private static string ToMemoryUrl(string memoryFileName)
    {
        var result = $"/memory/{Uri.EscapeDataString(memoryFileName)}";

        return result;
    }

    private string BuildClipboardPageUrl(string requestId)
    {
        var baseUrl = ResolveBaseUrl();
        var result = $"{baseUrl}{ClipboardPagePath}?id={Uri.EscapeDataString(requestId)}";

        return result;
    }

    private string ResolveBaseUrl()
    {
        var configuredUrl =
            configuration["AppSettings:PublicBaseUrl"]
            ?? configuration["PublicBaseUrl"]
            ?? configuration["urls"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
            ?? "http://localhost:9255/";

        var firstUrl =
            configuredUrl
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(url => url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            ?? "http://localhost:9255/";

        firstUrl = firstUrl.Replace("+", "localhost").Replace("*", "localhost");

        if (firstUrl.Contains("0.0.0.0", StringComparison.Ordinal))
        {
            firstUrl = firstUrl.Replace("0.0.0.0", "localhost", StringComparison.Ordinal);
        }

        return firstUrl.TrimEnd('/');
    }

    private sealed class MediaGroupBuffer
    {
        public ConcurrentDictionary<int, TelegramIncomingMessage> Messages { get; } = new();
        public int IsProcessed;
        public CancellationTokenSource? DebounceCts;

        public void ResetDebounce()
        {
            DebounceCts?.Cancel();
        }
    }

    private sealed class TriggerWaitBuffer
    {
        public bool HasTrigger;
        public CancellationTokenSource? TimeoutCts;
    }

    private sealed class ClipboardRequestFiles(string[] memoryFileNames, DateTimeOffset createdAt)
    {
        public string[] MemoryFileNames { get; } = memoryFileNames;
        public DateTimeOffset CreatedAt { get; } = createdAt;
    }
}
