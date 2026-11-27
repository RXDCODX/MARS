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

public enum CommandOutboxMessageType
{
    ExecuteCommand = 0,
    SendResponse = 1,
}

public enum CommandOutboxStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    NeedsReview = 4,
}