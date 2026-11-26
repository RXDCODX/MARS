using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Commands.Control;

namespace MARS.Platform.Infrastructure.Grpc.Commands;

internal sealed class CommandsApi(GrpcClientFactory clientFactory) : ICommandsApi
{
    public Task<InvokeCommandResponse> InvokeAsync(
        string commandName,
        string input,
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<CommandsControlService.CommandsControlServiceClient>("Commands")
            .InvokeAsync(
                new InvokeCommandRequest { CommandName = commandName, Input = input },
                cancellationToken: cancellationToken
            )
            .ResponseAsync;
}