using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class TtsvoiceCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "ttsvoice";

    public override string Description => "Переназначает голос TTS для пользователя";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var userId = parameters.TryGetValue("userId", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommandResult.Fail("Не удалось определить пользователя.", CommandErrorCode.BadArguments);
        }

        try
        {
            await grpcClient.Tts.ReassignVoiceAsync(userId, cancellationToken);
            return CommandResult.Ok("Голос TTS переназначен.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось переназначить голос TTS.", CommandErrorCode.TargetUnreachable);
        }
    }
}