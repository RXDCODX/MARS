using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Domain.Enums;

public enum CommandExecutionStatus
{
    Accepted = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    NeedsReview = 4,
    Cancelled = 5,
}
