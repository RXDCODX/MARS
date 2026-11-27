using Grpc.Core;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Adapters;
using MARS.Commands.Application.Services.Entitys;
using MARS.Shared.Contracts.Commands.Control;
using Microsoft.Extensions.Logging;
using ProtoCommands = MARS.Shared.Contracts.Commands.Control;

namespace MARS.Commands.Api.Grpc;

public sealed class CommandsControlGrpcService(
    ApiCommandService apiCommandService,
    ILogger<CommandsControlGrpcService> logger
) : CommandsControlService.CommandsControlServiceBase
{
    public override async Task<InvokeCommandResponse> Invoke(
        InvokeCommandRequest request,
        ServerCallContext context
    )
    {
        try
        {
            var response = await apiCommandService.ExecuteCommandAsync(
                request.CommandName,
                request.Input,
                context.CancellationToken
            );

            var grpcResponse = new InvokeCommandResponse
            {
                Success = response.Success,
                Text = response.Text,
                JobId = response.JobId ?? string.Empty,
                ErrorCode = (int)response.ErrorCode,
            };

            foreach (var attachment in response.Attachments)
            {
                grpcResponse.Attachments.Add(
                    new ProtoCommands.CommandAttachment
                    {
                        Kind = (int)attachment.Kind,
                        Url = attachment.Url,
                        Caption = attachment.Caption ?? string.Empty,
                    }
                );
            }

            return grpcResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {CommandName}", request.CommandName);
            return new ProtoCommands.InvokeCommandResponse
            {
                Success = false,
                Text = $"Error executing command: {ex.Message}",
            };
        }
    }
}
