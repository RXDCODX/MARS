using System;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;

namespace MARS.Commands.Application.Services;

public interface ICommandResponseSender
{
    Task SendAsync(CommandExecution execution, string responsePayload, CancellationToken cancellationToken);
}

public interface ICommandExecutionLedger
{
    Task<CommandExecution> AcceptAsync(CommandInvocation invocation, CancellationToken cancellationToken);

    Task<CommandExecution?> GetStatusAsync(Guid executionId, CancellationToken cancellationToken);
}