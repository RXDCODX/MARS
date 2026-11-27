using MARS.Platform.Infrastructure.Grpc;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.MTProto.Services;

/// <summary>
/// Загрузка файла во внутреннее memory-хранилище MARS.MediaStorage.
/// </summary>
public sealed class MediaStorageMemoryUploader(
    IMarsGrpcClient grpcClient,
    ILogger<MediaStorageMemoryUploader> logger
) : IMemoryMediaUploader
{
    public async Task<string?> UploadAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await grpcClient.MediaStorage.UploadMemoryFileAsync(
                fileName,
                content,
                cancellationToken
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Не удалось загрузить файл {FileName} в MARS.MediaStorage",
                fileName
            );
            return null;
        }
    }
}
