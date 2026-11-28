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
public class SpotifyauthStartCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "spotifyauthstart";

    public override string Description => "Запускает авторизацию Spotify";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Discord];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "redirectUri",
                Description = "URI перенаправления Spotify",
                Type = CommandParameterType.String,
                Required = false,
                DefaultValue = "http://localhost/callback",
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var redirectUri =
            parameters.TryGetValue("redirectUri", out var value)
            && !string.IsNullOrWhiteSpace(value?.ToString())
                ? value!.ToString()!.Trim()
                : "http://localhost/callback";

        try
        {
            var text = await grpcClient.SoundRequest.StartSpotifyAuthorizationAsync(
                redirectUri,
                cancellationToken
            );
            return CommandResult.Ok(
                string.IsNullOrWhiteSpace(text)
                    ? "Не удалось подготовить авторизацию Spotify."
                    : text
            );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось подготовить авторизацию Spotify.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
