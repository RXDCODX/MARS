using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class TtsvolumeCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "ttsvolume";

    public override string Description => "Показывает или меняет громкость TTS";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "volume",
                Description = "Громкость от 0 до 200",
                Type = CommandParameterType.Int,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        if (
            parameters.TryGetValue("volume", out var value)
            && int.TryParse(value?.ToString(), out var volume)
        )
        {
            var normalized = Math.Clamp(volume, 0, 200) / 100.0;
            try
            {
                await grpcClient.Tts.SetVolumeAsync(normalized, cancellationToken);
                return CommandResult.Ok($"Громкость TTS установлена: {volume}.");
            }
            catch (RpcException)
            {
                return CommandResult.Fail(
                    "Не удалось изменить громкость TTS.",
                    CommandErrorCode.TargetUnreachable
                );
            }
        }

        if (parameters.ContainsKey("volume"))
        {
            return CommandResult.Fail(
                "Громкость должна быть числом от 0 до 200.",
                CommandErrorCode.BadArguments
            );
        }

        try
        {
            var current = await grpcClient.Tts.GetVolumeAsync(cancellationToken);
            return CommandResult.Ok($"Текущая громкость TTS: {current * 100:0}.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось получить громкость TTS.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
