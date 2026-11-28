using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.MTProto.PrivateChannelsResender;
using MARS.Telegram.Infrastructure.MTProto.WTelegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;
using Wolverine;
using Document = TL.Document;
using Message = TL.Message;
using PhotoSize = TL.PhotoSize;
using Update = TL.Update;

namespace MARS.Telegram.Infrastructure.MTProto.Services;

/// <summary>
/// Служба случайных мемов: следит за указанными каналами Telegram через
/// пользовательский MTProto-клиент и при появлении фото/видео публикует событие
/// оверлея. Перенесена из MARS.Alerts, где жила отдельным юзерботом.
/// </summary>
[ExcludeFromCodeCoverage]
public class TelegramRandomMemService(
    ILogger<TelegramRandomMemService> logger,
    IWTelegramChannelClientProvider clientProvider,
    IOptions<TelegramConfiguration> config,
    IServiceScopeFactory scopeFactory,
    IMemoryMediaUploader uploader,
    IHostApplicationLifetime lifetime
) : BackgroundService, IRandomMemService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMemoryMediaUploader _uploader = uploader;
    private readonly FrozenSet<long> _allowedIds = config.Value.RandomMemChannelIds.ToFrozenSet();
    private IWTelegramChannelClient? _client;
    private bool _isEnabled = true;
    public bool IsEnabled => _isEnabled;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() => _ = InitializeAsync(stoppingToken));
        return Task.CompletedTask;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_allowedIds.Count == 0)
        {
            logger.LogInformation("TelegramRandomMem: список каналов пуст, чтение отключено");
            return;
        }

        await LoadEnabledStateAsync(cancellationToken);

        try
        {
            _client = await clientProvider.GetClientAsync(cancellationToken);
            _client.OnUpdates += OnUpdatesAsync;
            logger.LogInformation(
                "TelegramRandomMem: мониторинг {Count} каналов",
                _allowedIds.Count
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TelegramRandomMem: не удалось инициализировать клиент");
        }
    }

    private async Task OnUpdatesAsync(UpdatesBase @base)
    {
        if (!_isEnabled)
        {
            return;
        }
        if (@base is not Updates typed)
        {
            return;
        }

        foreach (var update in typed.updates)
        {
            if (update is not UpdateNewChannelMessage messageUpdate)
            {
                continue;
            }

            if (messageUpdate.message is not Message message)
            {
                continue;
            }

            if (!_allowedIds.Contains(message.peer_id.ID))
            {
                continue;
            }

            if (HasBadEntities(message))
            {
                continue;
            }

            try
            {
                await ProcessMessageAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramRandomMem: ошибка обработки сообщения");
            }
        }
    }

    private static bool HasBadEntities(Message message) =>
        message.entities.Any(e =>
            e
                is MessageEntityTextUrl
                    or MessageEntityUrl
                    or MessageEntityBankCard
                    or MessageEntityBotCommand
                    or MessageEntityCashtag
                    or MessageEntityCode
                    or MessageEntityEmail
                    or MessageEntityPhone
                    or MessageEntitySpoiler
                    or MessageEntityMention
                    or MessageEntityMentionName
        );

    private async Task ProcessMessageAsync(Message message)
    {
        switch (message.media)
        {
            case MessageMediaPhoto photo when photo.photo is Photo photoa:
                if (photoa.LargestPhotoSize is PhotoSize size)
                {
                    await ProcessPhotoAsync(photoa, size, message.message);
                }
                break;

            case MessageMediaDocument document when document.document is Document doc:
                var typeFile = doc.mime_type.Split('/')[1];
                if (Enum.TryParse(typeof(Storage_FileType), typeFile, true, out var value))
                {
                    var fileType = (Storage_FileType)value;
                    switch (fileType)
                    {
                        case Storage_FileType.jpeg or Storage_FileType.png or Storage_FileType.webp:
                            await ProcessPhotoDocumentAsync(doc, message.message);
                            break;
                        case Storage_FileType.gif or Storage_FileType.mov or Storage_FileType.mp4:
                            await ProcessVideoDocumentAsync(doc, message.message);
                            break;
                    }
                }
                break;
        }
    }

    private async Task ProcessVideoDocumentAsync(Document doc, string? messageText)
    {
        var buffer = new byte[doc.size];
        await using var fs = new MemoryStream(buffer);
        var fileInfo = await _client!.DownloadFileAsync(doc, fs);
        var extension = fileInfo.Split('/')[1];
        var fileName = $"{doc.id}.{extension}";

        var path = await _uploader.UploadAsync(fileName, buffer);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var relativePath = path.TrimStart('/');
        var mediaInfo = new MediaInfo
        {
            FileInfo = new MediaFileInfo
            {
                FilePath = relativePath,
                Extension = extension,
                FileName = fileName,
                Type = MediaType.Video,
            },
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = string.Empty,
                Duration = 999,
                IsLooped = false,
                Priority = MediaAlertPriority.Normal,
            },
            PositionInfo = new MediaPositionInfo(),
            StylesInfo = new MediaStylesInfo { IsBorder = true },
            TextInfo = new MediaTextInfo { Text = messageText },
        };

        await PublishAsync(mediaInfo, messageText);
    }

    private async Task ProcessPhotoAsync(Photo photo, PhotoSize size, string? messageText)
    {
        var buffer = new byte[size.FileSize];
        await using var fs = new MemoryStream(buffer);
        var fileInfo = await _client!.DownloadFileAsync(photo, fs, size);
        var fileName = $"{photo.id}.jpeg";

        var path = await _uploader.UploadAsync(fileName, buffer);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var relativePath = path.TrimStart('/');
        var mediaInfo = new MediaInfo
        {
            FileInfo = new MediaFileInfo
            {
                FilePath = relativePath,
                Extension = "jpeg",
                FileName = fileName,
                Type = MediaType.Image,
            },
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = string.Empty,
                IsLooped = false,
                Priority = MediaAlertPriority.Normal,
            },
            PositionInfo = new MediaPositionInfo(),
            StylesInfo = new MediaStylesInfo(),
            TextInfo = new MediaTextInfo { Text = messageText },
        };

        await PublishAsync(mediaInfo, messageText);
    }

    private async Task ProcessPhotoDocumentAsync(Document document, string? messageText)
    {
        var buffer = new byte[document.size];
        await using var fs = new MemoryStream(buffer);
        var fileInfo = await _client!.DownloadFileAsync(document, fs);
        var extension = fileInfo.Split('/')[1];
        var fileName = $"{document.id}.{extension}";

        var path = await _uploader.UploadAsync(fileName, buffer);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var relativePath = path.TrimStart('/');
        var mediaInfo = new MediaInfo
        {
            FileInfo = new MediaFileInfo
            {
                FilePath = relativePath,
                Extension = extension,
                FileName = fileName,
                Type = MediaType.Image,
            },
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = string.Empty,
                IsLooped = false,
                Priority = MediaAlertPriority.Normal,
            },
            PositionInfo = new MediaPositionInfo(),
            StylesInfo = new MediaStylesInfo(),
            TextInfo = new MediaTextInfo { Text = messageText },
        };

        await PublishAsync(mediaInfo, messageText);
    }

    private async Task PublishAsync(MediaInfo mediaInfo, string? messageText)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new RandomMemRequested { Media = mediaInfo });
    }

    public override void Dispose()
    {
        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.Dispose();
    }

    public async Task<bool> SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default
    )
    {
        _isEnabled = enabled;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db =
            scope.ServiceProvider.GetRequiredService<MARS.Telegram.Infrastructure.Postgres.Data.ChatDbContext>();
        var rootState = await db.RootState.FirstOrDefaultAsync(
            x => x.Name == "RandomMemOnlineIsStop",
            cancellationToken
        );

        if (rootState is null)
        {
            db.RootState.Add(
                new MARS.Telegram.Domain.Entities.RootState
                {
                    Name = "RandomMemOnlineIsStop",
                    Value = (!enabled).ToString(),
                    Description = "Random mem online is stopped",
                    TypeDescription = "bool",
                }
            );
        }
        else
        {
            rootState.Value = (!enabled).ToString();
        }

        await db.SaveChangesAsync(cancellationToken);
        return _isEnabled;
    }

    private async Task LoadEnabledStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db =
                scope.ServiceProvider.GetRequiredService<MARS.Telegram.Infrastructure.Postgres.Data.ChatDbContext>();
            var value = await db
                .RootState.AsNoTracking()
                .Where(x => x.Name == "RandomMemOnlineIsStop")
                .Select(x => x.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (bool.TryParse(value, out var isStopped))
            {
                _isEnabled = !isStopped;
            }
        }
        catch
        {
            // Keep default enabled state if RootState is unavailable.
        }
    }
}
