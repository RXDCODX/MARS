using System;

namespace MARS.Commands.Application.Services.Entitys;

public sealed record CommandInvocation(
    string RequestId,
    string Platform,
    string CommandName,
    string Input,
    string CallerId,
    string? CallerDisplayName,
    bool IsAdmin,
    bool IsIdempotent,
    string? ReplyTo = null,
    string? SourceMessageId = null,
    string? InlineMessageId = null
);