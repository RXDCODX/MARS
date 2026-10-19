using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Telegram.Configuration;
using MARS.Telegram.Models;
using MARS.Telegram.Services.BotService;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Telegram.Services.GooglePhotos;

/// <summary>
/// Выгрузка фотографий из Telegram в Google Photos.
/// </summary>
/// <remarks>
/// Работает по событию входящего сообщения, а не по опросу Bot API: события
/// приходят из сессии MTProto. Из Bot API здесь осталось только имя чата —
/// настройка та же.
/// </remarks>
public class TelegramGooglePhotosService(
    ILogger<TelegramGooglePhotosService> logger,
    IGooglePhotosAuthService authService,
    GooglePhotosApiClient apiClient,
    IHostApplicationLifetime applicationLifetime,
    IOptions<GooglePhotosConfiguration> googlePhotosOptions
) : ITelegramusService, ITelegramIncomingHandler
{
    private readonly GooglePhotosConfiguration _config = googlePhotosOptions.Value;
    private CancellationToken StoppingToken => applicationLifetime.ApplicationStopping;

    /// <inheritdoc />
    public Task HandleAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        CancellationToken cancellationToken
    )
    {
        var result = Task.CompletedTask;

        if (message.ChatId == _config.TelegramChatId)
        {
            result = HandleMessageAsync(client, message);
        }

        return result;
    }

    private async Task HandleMessageAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message
    )
    {
        var isAuthorized = await authService.IsAuthorizedAsync(StoppingToken);

        if (!isAuthorized)
        {
            await SendTextAsync(client, message.Peer, "❌ Google Photos не авторизован");
            return;
        }

        if (message.Photo is not null)
        {
            await ProcessPhotosAsync(client, message);
        }
        else if (message.Document is { } document && IsImage(document))
        {
            await ProcessDocumentAsync(client, document);
        }
    }

    /// <summary>
    /// Документ считается картинкой по типу содержимого: без него в Google Photos
    /// уехал бы файл, который сервис там не откроет.
    /// </summary>
    private static bool IsImage(Document document) =>
        document.mime_type?.StartsWith("image/", StringComparison.Ordinal) == true;

    private async Task ProcessPhotosAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message
    )
    {
        try
        {
            var photo = message.Photo;
            var size = LargestSize(photo);

            if (photo is null || size is null)
            {
                await SendTextAsync(client, message.Peer, "❌ Нет фото");
                return;
            }

            await using var photoStream = new MemoryStream();
            await client.DownloadFileAsync(photo, photoStream, size, StoppingToken);
            photoStream.Position = 0;

            var fileName = $"telegram_{message.MessageId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var uploadResult = await apiClient.UploadPhotoAsync(
                photoStream,
                fileName,
                StoppingToken
            );

            if (uploadResult.Success)
            {
                logger.LogInformation("Фото загружено: {MediaItemId}", uploadResult.Result);
                await SendTextAsync(
                    client,
                    message.Peer,
                    $"✅ Фото успешно загружено в Google Photos"
                );
            }
            else
            {
                logger.LogError("Ошибка загрузки: {Error}", uploadResult.ErrorMessage);
                await SendTextAsync(client, message.Peer, $"❌ {uploadResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки фото");
            await SendTextAsync(client, message.Peer, $"❌ Ошибка: {ex.Message}");
        }
    }

    private async Task ProcessDocumentAsync(ITelegramBotConnection client, Document document)
    {
        try
        {
            await using var documentStream = new MemoryStream();
            await client.DownloadFileAsync(document, documentStream, StoppingToken);
            documentStream.Position = 0;

            var fileName = FileNameOf(document) ?? $"document_{DateTime.Now:yyyyMMdd_HHmmss}";
            var uploadResult = await apiClient.UploadPhotoAsync(
                documentStream,
                fileName,
                StoppingToken
            );

            if (uploadResult.Success)
            {
                logger.LogInformation("Документ загружен: {MediaItemId}", uploadResult.Result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки документа");
        }
    }

    /// <summary>
    /// Наибольший размер фото: Telegram присылает несколько, и первый — это
    /// уменьшенная копия, в Google Photos такая бесполезна.
    /// </summary>
    private static PhotoSize? LargestSize(Photo? photo)
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
    /// Имя файла лежит в атрибутах документа, а не в самом документе: у Bot API
    /// было поле <c>FileName</c>, в MTProto такого поля нет.
    /// </summary>
    private static string? FileNameOf(Document document)
    {
        string? result = null;

        foreach (var attribute in document.attributes)
        {
            if (attribute is DocumentAttributeFilename { file_name: { } name })
            {
                result = name;
            }
        }

        return result;
    }

    /// <summary>
    /// Ответ без разметки: сообщения сервиса односложные, и разбирать в них нечего.
    /// </summary>
    private static Task SendTextAsync(ITelegramBotConnection client, InputPeer peer, string text) =>
        client.SendTextAsync(peer, text, entities: null);
}
