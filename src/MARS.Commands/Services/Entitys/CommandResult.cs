namespace MARS.Commands.Services.Entitys;

/// <summary>
/// Результат выполнения команды. Заменяет <c>Task&lt;string&gt;</c>: строка не
/// различала успех и отказ и не несла вложений.
///
/// Форма совпадает с <c>InvokeResponse</c> в <c>commands.proto</c>: на gRPC
/// ошибки уходят статусом вызова, а этот тип описывает именно успех.
/// </summary>
public sealed record CommandResult
{
    private CommandResult(
        bool success,
        string text,
        CommandErrorCode errorCode,
        string? jobId,
        IReadOnlyList<CommandAttachment> attachments
    )
    {
        Success = success;
        Text = text;
        ErrorCode = errorCode;
        JobId = jobId;
        Attachments = attachments;
    }

    public bool Success { get; }

    public string Text { get; init; }

    public CommandErrorCode ErrorCode { get; init; }

    /// <summary>Непустой у асинхронной команды: результат придёт событием.</summary>
    public string? JobId { get; init; }

    public IReadOnlyList<CommandAttachment> Attachments { get; init; }

    public static CommandResult Ok(string text) =>
        new(true, text, CommandErrorCode.None, null, []);

    public static CommandResult Ok(string text, IReadOnlyList<CommandAttachment> attachments) =>
        new(true, text, CommandErrorCode.None, null, attachments);

    public static CommandResult Fail(string text, CommandErrorCode errorCode) =>
        new(false, text, errorCode, null, []);

    /// <summary>
    /// Асинхронная команда принята: результат придёт событием с этим
    /// <paramref name="jobId"/>, по нему и сопоставляется.
    /// </summary>
    public static CommandResult Accepted(string text, string jobId) =>
        new(true, text, CommandErrorCode.Accepted, jobId, []);
}
