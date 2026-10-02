using MARS.MediaStorage.Extensions;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests.Extensions;

/// <summary>
/// Подстановки в текст и цвет оповещения.
///
/// Шаблон алерта пишет автор, а в тексте зрителя стоят плейсхолдеры. Проверяется
/// подстановка без слеша: у автора сообщение приходит как «@имя», и вместе со
/// слешем уехало бы и упоминание.
/// </summary>
public class MediaInfoExtensionTests
{
    /// <summary>
    /// У сообщения убирается только ведущий слеш упоминания: «@pyro привет»
    /// становится «pyro привет», а не «привет» — имя автора остаётся в тексте
    /// оповещения.
    /// </summary>
    [Fact]
    public void MentionSignIsStrippedFromUserText()
    {
        var media = Media(text: "{user.text}: спасибо");

        media.FixAlertText("Pyro", "@pyro привет");

        Assert.Equal("pyro привет: спасибо", media.TextInfo.Text);
    }

    /// <summary>
    /// Текст без упоминания подставляется как есть: лишний пробел в оповещении был бы
    /// виден всем.
    /// </summary>
    [Fact]
    public void PlainUserTextIsTrimmed()
    {
        var media = Media(text: "{user.text}");

        media.FixAlertText("Pyro", "  привет  ");

        Assert.Equal("привет", media.TextInfo.Text);
    }

    [Fact]
    public void UserNameIsSubstituted()
    {
        var media = Media(text: "{user.name} попросил");

        media.FixAlertText("Pyro", "привет");

        Assert.Equal("Pyro попросил", media.TextInfo.Text);
    }

    /// <summary>
    /// Текст без плейсхолдеров не меняется: иначе алерт с собственным текстом
    /// потерял бы форматирование.
    /// </summary>
    [Fact]
    public void TextWithoutPlaceholdersIsKept()
    {
        var media = Media(text: "просто текст");

        media.FixAlertText("Pyro", "привет");

        Assert.Equal("просто текст", media.TextInfo.Text);
    }

    /// <summary>
    /// Цвет пользователя подставляется в текст сообщения и в цвет текста: оба
    /// показываются на экране.
    /// </summary>
    [Fact]
    public void ChatColorIsSubstituted()
    {
        var media = Media(text: "привет");
        media.TextInfo.TextColor = "{user.color}";

        media.FixAlertColor("#FF0000");

        Assert.Equal("#FF0000", media.TextInfo.TextColor);
    }

    /// <summary>
    /// Цвет ключевых слов заменяется, когда в нём есть плейсхолдер: иначе в чате
    /// печаталась бы сама строка «{user.color}».
    /// </summary>
    [Fact]
    public void KeywordColorIsSubstituted()
    {
        var media = Media(text: "привет");
        media.TextInfo.KeyWordsColor = "цвет: {user.color}";

        media.FixAlertColor("#00FF00");

        Assert.Equal("цвет: #00FF00", media.TextInfo.KeyWordsColor);
    }

    /// <summary>
    /// Обычный текст и цвет не трогаются: подменять нечего.
    /// </summary>
    [Fact]
    public void TextAndColorWithoutPlaceholdersAreKept()
    {
        var media = Media(text: "привет");
        media.TextInfo.TextColor = "#FFFFFF";
        media.TextInfo.KeyWordsColor = "#FFFF00";

        media.FixAlertColor("#FF0000");

        Assert.Equal("привет", media.TextInfo.Text);
        Assert.Equal("#FFFFFF", media.TextInfo.TextColor);
        Assert.Equal("#FFFF00", media.TextInfo.KeyWordsColor);
    }

    private static MediaInfo Media(string text) =>
        new()
        {
            TextInfo = new MediaTextInfo { Text = text },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/a.mp3",
                FileName = "a",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "аяка" },
            StylesInfo = new MediaStylesInfo(),
        };
}
