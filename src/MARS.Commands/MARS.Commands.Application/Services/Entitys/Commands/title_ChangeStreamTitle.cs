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
public class TitleCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "title";

    public override string Description => "Показывает или меняет название трансляции";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override string[] Aliases => [];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "новое_название",
                Description = "Новое название трансляции",
                Type = CommandParameterType.String,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var newTitle = parameters.TryGetValue("новое_название", out var value)
            ? value?.ToString()?.Trim() ?? string.Empty
            : string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                var title = await grpcClient.Twitch.GetStreamTitleAsync(cancellationToken);
                return CommandResult.Ok(
                    string.IsNullOrWhiteSpace(title)
                        ? "Название трансляции не задано."
                        : $"Текущее название: {title}"
                );
            }

            if (newTitle.Length > 140)
            {
                newTitle = newTitle[..140];
            }

            var success = await grpcClient.Twitch.ChangeStreamTitleAsync(newTitle, cancellationToken);
            return success
                ? CommandResult.Ok($"Название трансляции изменено: {newTitle}")
                : CommandResult.Fail("Не удалось изменить название трансляции.", CommandErrorCode.TargetUnreachable);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось получить или изменить название трансляции.", CommandErrorCode.TargetUnreachable);
        }
    }
}