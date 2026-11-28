using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services;

public interface ICommandResponseSender
{
    Task SendAsync(CommandExecution execution, string responsePayload, CancellationToken cancellationToken);
}