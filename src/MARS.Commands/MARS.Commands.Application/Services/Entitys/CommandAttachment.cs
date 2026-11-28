using System.Diagnostics.CodeAnalysis;
namespace MARS.Commands.Application.Services.Entitys;

/// <summary>
/// Вложение в ответе команды. Голой строки для этого не хватало: команда
/// <c>download</c> возвращает файл, и до <see cref="CommandResult"/> ему некуда
/// было его положить.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CommandAttachment(
    CommandAttachmentKind Kind,
    string Url,
    string? Caption = null,
    string? FileName = null,
    string? MimeType = null,
    double? DurationSeconds = null,
    int? Width = null,
    int? Height = null
);
