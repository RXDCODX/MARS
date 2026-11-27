using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class CatisaCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "catisa";

    public override string Description => "Отправляет сообщение в указанный Twitch-канал";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Twitch, Platform.Discord];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "channel",
                Description = "Имя канала",
                Type = CommandParameterType.String,
                Required = true,
            },
            new()
            {
                Name = "text",
                Description = "Текст сообщения",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var channel = parameters.TryGetValue("channel", out var channelValue) ? channelValue?.ToString()?.Trim() ?? string.Empty : string.Empty;
        var text = parameters.TryGetValue("text", out var textValue) ? textValue?.ToString()?.Trim() ?? string.Empty : string.Empty;

        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(text))
        {
            return CommandResult.Fail("Не указан канал или текст.", CommandErrorCode.BadArguments);
        }

        try
        {
            var success = await grpcClient.Twitch.JoinChannelAndSendAsync(channel, text, cancellationToken);
            return success
                ? CommandResult.Ok($"Сообщение отправлено в канал {channel}.")
                : CommandResult.Fail("Не удалось отправить сообщение.", CommandErrorCode.TargetUnreachable);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось отправить сообщение.", CommandErrorCode.TargetUnreachable);
        }
    }
}