using System.Diagnostics.CodeAnalysis;
namespace MARS.Commands.Domain.Enums;

public enum CommandOutboxMessageType
{
    ExecuteCommand = 0,
    SendResponse = 1,
}