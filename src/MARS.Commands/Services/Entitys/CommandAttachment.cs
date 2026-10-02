namespace MARS.Commands.Services.Entitys;

/// <summary>
/// Вложение в ответе команды. Голой строки для этого не хватало: команда
/// <c>download</c> возвращает файл, и до <see cref="CommandResult"/> ему некуда
/// было его положить.
/// </summary>
public sealed record CommandAttachment(
    CommandAttachmentKind Kind,
    string Url,
    string? Caption = null
);

/// <summary>Тип вложения.</summary>
public enum CommandAttachmentKind
{
    Image,
    Video,
    Audio,
    File,
}
