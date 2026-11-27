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
    private readonly IMarsGrpcClient _grpcClient = grpcClient;

    public override string CommandName => "googlephotosauthorize";

    public override string Description => "Получает ссылку авторизации Google Photos";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Discord];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            CommandResult.Fail("Команда временно недоступна.", CommandErrorCode.TargetUnreachable)
        );
    }
}
