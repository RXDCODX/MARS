using System;

namespace MARS.Commands.Application.Services.Entitys;

public sealed record CommandResponsePayload(
    string Platform,
    string? ReplyTo,
    string? SourceMessageId,
    string Text,
    IReadOnlyList<MARS.Commands.Application.Services.Entitys.CommandAttachment>? Attachments = null
);
