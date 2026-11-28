using System;
using System.Diagnostics.CodeAnalysis;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;

namespace MARS.Commands.Application.Services;

public interface ICommandExecutionLedger
{
    Task<CommandExecution> AcceptAsync(
        CommandInvocation invocation,
        CancellationToken cancellationToken
    );

    Task<CommandExecution?> GetStatusAsync(Guid executionId, CancellationToken cancellationToken);
}
