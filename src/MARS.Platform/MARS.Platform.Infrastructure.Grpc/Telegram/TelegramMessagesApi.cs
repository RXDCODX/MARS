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

    public async Task<string> GetGooglePhotosAuthorizationUrlAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<TelegramMessageService.TelegramMessageServiceClient>("TelegramMessage")
            .GetGooglePhotosAuthorizationUrlAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Url;
    }
}
