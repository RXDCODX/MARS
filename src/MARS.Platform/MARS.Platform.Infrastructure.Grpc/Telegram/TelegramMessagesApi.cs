using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.TelegramMessage;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

internal sealed class TelegramMessagesApi(GrpcClientFactory clientFactory) : ITelegramMessagesApi
{
    public async Task<bool> SendAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .SendAsync(
                new SendMessageRequest { ChatId = chatId, Text = text },
                cancellationToken: cancellationToken
            );

        return response.Delivered;
    }

    public async Task<bool> SendMediaAsync(
        long chatId,
        string fileUrl,
        string fileName,
        string mimeType,
        string caption,
        long replyToMessageId,
        int mediaKind,
        double durationSeconds,
        int width,
        int height,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .SendMediaAsync(
                new SendMediaRequest
                {
                    ChatId = chatId,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    MimeType = mimeType,
                    Caption = caption,
                    ReplyToMessageId = replyToMessageId,
                    MediaKind = mediaKind,
                    DurationSeconds = durationSeconds,
                    Width = width,
                    Height = height,
                },
                cancellationToken: cancellationToken
            );

        return response.Delivered;
    }

    public async Task<string> GetGooglePhotosAuthorizationUrlAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .GetGooglePhotosAuthorizationUrlAsync(
                new Empty(),
                cancellationToken: cancellationToken
            );
        return response.Url;
    }

    public async Task SetRandomMemEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default
    )
    {
        await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .SetRandomMemEnabledAsync(
                new RandomMemEnabledRequest { Enabled = enabled },
                cancellationToken: cancellationToken
            );
    }

    public async Task<IReadOnlyList<string>> GetWhitelistAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .GetWhitelistAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Users;
    }

    public async Task<string> GetWTelegramStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .GetWTelegramStatusAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Status;
    }
}
