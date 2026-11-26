using MARS.Shared.Contracts.Commands.Control;

namespace MARS.Platform.Infrastructure.Grpc.Commands;

public interface ICommandsApi
{
    Task<InvokeCommandResponse> InvokeAsync(
        string commandName,
        string input,
        CancellationToken cancellationToken = default
    );
}