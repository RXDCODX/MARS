using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class GooglephotosAuthorizeCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "googlephotosauthorize";

    public override string Description => "Получает ссылку авторизации Google Photos";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Discord];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url = await grpcClient.Telegram.Messages.GetGooglePhotosAuthorizationUrlAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(url)
                ? CommandResult.Fail("Не удалось получить ссылку авторизации Google Photos.", CommandErrorCode.TargetUnreachable)
                : CommandResult.Ok($"Ссылка для авторизации Google Photos: {url}");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось получить ссылку авторизации Google Photos.", CommandErrorCode.TargetUnreachable);
        }
    }
}