using MARS.Commands.Services;
using MARS.Commands.Services.Entitys;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Результат команды перестаёт быть голой строкой. Строкой «нет такой команды»,
/// «мало аргументов» и «нет прав» были неотличимы друг от друга, и платформа не
/// могла ответить вызывающему осмысленно.
/// </summary>
public sealed class CommandResultTests
{
    [Fact]
    public void OkCarriesTextAndNoError()
    {
        var result = CommandResult.Ok("готово");

        Assert.True(result.Success);
        Assert.Equal("готово", result.Text);
        Assert.Equal(CommandErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void FailKeepsErrorCode()
    {
        var result = CommandResult.Fail("нет прав", CommandErrorCode.NotAllowed);

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.NotAllowed, result.ErrorCode);
    }

    /// <summary>
    /// Асинхронная команда возвращает принятие и идентификатор задачи: результат
    /// придёт событием, и без <c>job_id</c> его не с чем сопоставить.
    /// </summary>
    [Fact]
    public void AcceptedCarriesJobId()
    {
        var result = CommandResult.Accepted("поставлено в очередь", "job-7");

        Assert.True(result.Success);
        Assert.Equal("job-7", result.JobId);
        Assert.Equal(CommandErrorCode.Accepted, result.ErrorCode);
    }

    [Fact]
    public void ResultHasNoAttachmentsByDefault()
    {
        Assert.Empty(CommandResult.Ok("готово").Attachments);
    }

    /// <summary>
    /// Вложения — часть успешного результата: команда <c>download</c> возвращает
    /// файл, и без <c>Attachments</c> вызывающий получал текст без единого
    /// указания, что скачивать.
    /// </summary>
    [Fact]
    public void OkCarriesAttachments()
    {
        var attachment = new CommandAttachment(
            CommandAttachmentKind.Video,
            "https://mars.example.org/clip.mp4",
            "клип"
        );

        var result = CommandResult.Ok("готово", [attachment]);

        Assert.True(result.Success);
        Assert.Equal(attachment, Assert.Single(result.Attachments));
        Assert.Equal(CommandAttachmentKind.Video, result.Attachments[0].Kind);
        Assert.Equal("клип", result.Attachments[0].Caption);
    }

    /// <summary>
    /// Успех и ошибка не должны совпадать по коду: иначе платформа потеряет
    /// различие между «вложений нет» и «ошибка».
    /// </summary>
    [Fact]
    public void DefaultSuccessHasNoErrorCode()
    {
        Assert.NotEqual(CommandErrorCode.Failed, CommandResult.Ok("ок").ErrorCode);
    }
}
