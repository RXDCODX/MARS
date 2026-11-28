using System.Diagnostics.CodeAnalysis;
namespace MARS.Commands.Domain.Enums;

public enum CommandOutboxStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    NeedsReview = 4,
}