using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class RandommemCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    private readonly IMarsGrpcClient _grpcClient = grpcClient;

    public override string CommandName => "randommem";

    public override string Description => "Включает или выключает отправку случайных мемов";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

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
