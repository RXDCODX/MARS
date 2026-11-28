using MARS.Platform.Infrastructure.Http.MediaArtifact;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.MTProto.BotService;
using Microsoft.Extensions.Logging;
using TL;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Доставка медиа-вложений ботом по MTProto.
/// </summary>
public sealed class MTProtoMediaSender(
    ITelegramBotSession session,
    TelegramPeerResolver peerResolver,
    IMediaArtifactDownloader downloader,
    ILogger<MTProtoMediaSender> logger
) : ITelegramMediaSender
{
    private const long MaxBytes = 50L * 1024 * 1024;

    /// <inheritdoc />
    public async Task<bool> SendMediaAsync(
        long chatId,
        string url,
        string fileName,
        string mimeType,
        string caption,
        long replyToMessageId,
        int kind,
        double duration,
        int width,
        int height,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var connection = await session.GetAsync(cancellationToken);

            var peer = await peerResolver.ResolveAsync(chatId, connection, cancellationToken);
            if (peer is null)
            {
                logger.LogError(
                    "Медиа в Telegram не отправлено: не удалось определить адресат чата {ChatId}",
                    chatId
                );
                return false;
            }

            var uploadName = string.IsNullOrWhiteSpace(fileName) ? "file" : fileName;
            await using var stream = await downloader.DownloadAsync(
                url,
                MaxBytes,
                cancellationToken
            );
            var inputFile = await connection.UploadFileAsync(stream, uploadName);

            var isImage =
                kind == 0
                || (
                    !string.IsNullOrWhiteSpace(mimeType)
                    && mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                );

            InputMedia media;
            if (isImage)
            {
                media = new InputMediaUploadedPhoto { file = inputFile };
            }
            else
            {
                var attributes = new List<DocumentAttribute>();
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    attributes.Add(new DocumentAttributeFilename { file_name = fileName });
                }

                if (kind == 1)
                {
                    attributes.Add(
                        new DocumentAttributeVideo
                        {
                            duration = duration,
                            w = width,
                            h = height,
                        }
                    );
                }

                media = new InputMediaUploadedDocument
                {
                    file = inputFile,
                    mime_type = mimeType ?? string.Empty,
                    attributes = attributes.ToArray(),
                };
            }

            await connection.SendMediaAsync(
                peer,
                media,
                caption ?? string.Empty,
                null,
                replyToMessageId,
                cancellationToken
            );

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Медиа в Telegram не отправлено в чат {ChatId}", chatId);
            return false;
        }
    }
}
