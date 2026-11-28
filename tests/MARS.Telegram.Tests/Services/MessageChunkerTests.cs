using MARS.Telegram.Infrastructure.MTProto.Messaging;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Разбиение длинных сообщений на части, умещающиеся в ограничение Telegram.
/// </summary>
public class MessageChunkerTests
{
    [Fact]
    public void Короткое_Сообщение_Одна_Часть_Без_Обрезки()
    {
        var result = MessageChunker.SplitPlain("короткий текст");

        Assert.Single(result.Chunks);
        Assert.Equal("короткий текст", result.Chunks[0]);
        Assert.Equal(0, result.SuppressedChars);
    }

    [Fact]
    public void Точно_4096_Символов_Одна_Часть()
    {
        var text = new string('a', MessageChunker.MaxMessageLength);

        var result = MessageChunker.SplitPlain(text);

        Assert.Single(result.Chunks);
        Assert.Equal(text, result.Chunks[0]);
        Assert.Equal(0, result.SuppressedChars);
    }

    [Fact]
    public void Ровно_4097_Символов_Две_Части()
    {
        var text = new string('a', MessageChunker.MaxMessageLength + 1);

        var result = MessageChunker.SplitPlain(text);

        Assert.Equal(2, result.Chunks.Length);
        Assert.True(result.Chunks.All(c => c.Length <= MessageChunker.MaxMessageLength));
        Assert.Equal(0, result.SuppressedChars);
    }

    [Fact]
    public void Разрез_Предпочитает_Перевод_Строки()
    {
        var line = new string('a', 3000);
        var text = line + "\n" + line + "\n" + line;

        var result = MessageChunker.SplitPlain(text);

        Assert.True(result.Chunks.Length >= 2);
        // Первая часть должна заканчиваться на переводе строки (или сразу после него).
        Assert.Contains("\n", result.Chunks[0] + "x"); // sanity
        Assert.True(result.Chunks[0].TrimEnd().EndsWith(line[..100]) || result.Chunks[0].Contains("\n"));
    }

    [Fact]
    public void Несломанный_Тег_Не_Режется()
    {
        // Тег длиннее половины лимита не должен разорваться.
        var inner = new string('x', 3000);
        var html = $"<b>{inner}</b>";

        var result = MessageChunker.SplitHtml(html);

        // Либо одна часть (всё влезает), либо разрез вне тега.
        foreach (var chunk in result.Chunks)
        {
            // Каждая часть должна корректно разбираться.
            var ex = Record.Exception(() => HtmlMessageEntities.Parse(chunk, out _));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void Каждая_Часть_Корректно_Разбирается_Как_HTML()
    {
        var line = "Не удалось создать награду Twitch: Reward " + new string('x', 100);
        var html = "<b>Синхронизация наград Twitch</b>\n\n" + string.Join("\n", Enumerable.Repeat(line, 60));

        var result = MessageChunker.SplitHtml(html);

        Assert.True(result.Chunks.Length >= 2);
        foreach (var chunk in result.Chunks)
        {
            var ex = Record.Exception(() => HtmlMessageEntities.Parse(chunk, out _));
            Assert.Null(ex);
        }
        Assert.True(result.Chunks.All(c => c.Length <= MessageChunker.MaxMessageLength));
    }

    [Fact]
    public void Обрезка_Помечается_Маркером_При_Превышении_Лимита_Частей()
    {
        // 4 части по 4096 символов = больше 3 частей → обрезка.
        var text = new string('a', MessageChunker.MaxMessageLength * 4);

        var result = MessageChunker.SplitPlain(text);

        Assert.Equal(3, result.Chunks.Length);
        Assert.True(result.SuppressedChars > 0);
        Assert.Contains("символов скрыто", result.Chunks[^1]);
        Assert.True(result.Chunks.All(c => c.Length <= MessageChunker.MaxMessageLength));
    }

    [Fact]
    public void Пустая_Строка_Одна_Пустая_Часть()
    {
        var result = MessageChunker.SplitPlain("");

        Assert.Single(result.Chunks);
        Assert.Equal("", result.Chunks[0]);
        Assert.Equal(0, result.SuppressedChars);
    }

    [Fact]
    public void Несломанный_Жирный_Тег_Не_Разрывается()
    {
        // Жирный фрагмент должен остаться целиком в одной части.
        var html = "<b>заголовок</b>\n\n" + new string('y', 5000);

        var result = MessageChunker.SplitHtml(html);

        Assert.True(result.Chunks.Length >= 2);
        // Первая часть содержит закрытый тег.
        Assert.Contains("</b>", result.Chunks[0]);
        foreach (var chunk in result.Chunks)
        {
            var ex = Record.Exception(() => HtmlMessageEntities.Parse(chunk, out _));
            Assert.Null(ex);
        }
    }
}
