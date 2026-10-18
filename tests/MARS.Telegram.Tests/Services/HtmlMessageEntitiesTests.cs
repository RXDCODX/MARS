using MARS.Telegram.Services.Messaging;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Разбор разметки Telegram в сущности сообщения.
/// </summary>
/// <remarks>
/// У Bot API разметку разбирал Telegram по <c>ParseMode.Html</c>, и сервис отдавал
/// строку с тегами. По MTProto уходит готовый массив сущностей, поэтому разбор —
/// наш код, и ошибка в нём видна администратору прямо в чате.
/// </remarks>
public class HtmlMessageEntitiesTests
{
    /// <summary>
    /// Теги не попадают в текст, а становятся сущностями со смещениями в его
    /// координатах: иначе Telegram показал бы пользователю <c>&lt;b&gt;</c>.
    /// </summary>
    [Fact]
    public void Жирный_Текст_Становится_Сущностью()
    {
        var text = HtmlMessageEntities.Parse("готово <b>всё</b> ок", out var entities);

        Assert.Equal("готово всё ок", text);
        var entity = Assert.IsType<MessageEntityBold>(Assert.Single(entities));
        Assert.Equal(7, entity.offset);
        Assert.Equal(3, entity.length);
    }

    [Theory]
    [InlineData("i", typeof(MessageEntityItalic))]
    [InlineData("u", typeof(MessageEntityUnderline))]
    [InlineData("s", typeof(MessageEntityStrike))]
    [InlineData("code", typeof(MessageEntityCode))]
    [InlineData("pre", typeof(MessageEntityPre))]
    public void Каждый_Известный_Тег_Имеет_Свою_Сущность(string tag, Type expected)
    {
        var html = $"текст <{tag}>внутри</{tag}>";

        var text = HtmlMessageEntities.Parse(html, out var entities);

        Assert.Equal("текст внутри", text);
        Assert.IsType(expected, Assert.Single(entities));
    }

    /// <summary>
    /// Ссылка несёт адрес из атрибута, а метка остаётся текстом: адрес, взятый из
    /// самого тега, был бы пустым, и открыть ссылку было бы нечем.
    /// </summary>
    [Fact]
    public void Ссылка_Берёт_Адрес_Из_Атрибута()
    {
        var html = "<a href=\"https://example.org/page?id=7\">Открыть</a>";

        var text = HtmlMessageEntities.Parse(html, out var entities);

        Assert.Equal("Открыть", text);
        var entity = Assert.IsType<MessageEntityTextUrl>(Assert.Single(entities));
        Assert.Equal("https://example.org/page?id=7", entity.url);
        Assert.Equal(0, entity.offset);
        Assert.Equal(7, entity.length);
    }

    /// <summary>
    /// Вложенные теги дают вложенные сущности со своими смещениями, а не одну
    /// общую: смещение считается в координатах итогового текста.
    /// </summary>
    [Fact]
    public void Вложенные_Теги_Считают_Смещения_По_Итоговому_Тексту()
    {
        var html = "<b>жирный <i>и курсив</i></b>";

        var text = HtmlMessageEntities.Parse(html, out var entities);

        Assert.Equal("жирный и курсив", text);
        Assert.Equal(2, entities.Length);

        // Сущности закрываются изнутри наружу, поэтому внутренний тег в списке
        // первый: порядок иначе сбил бы смещения у внешнего.
        var italic = Assert.IsType<MessageEntityItalic>(entities[0]);
        Assert.Equal(7, italic.offset);
        Assert.Equal(8, italic.length);

        var bold = Assert.IsType<MessageEntityBold>(entities[1]);
        Assert.Equal(0, bold.offset);
        Assert.Equal(15, bold.length);
    }

    /// <summary>
    /// Неизвестный тег — отказ, а не «отправим как есть»: сырая разметка уехала бы
    /// в чат и её заметили бы уже в проде.
    /// </summary>
    [Fact]
    public void Неизвестный_Тег_Отклоняется()
    {
        var error = Assert.Throws<FormatException>(() =>
            HtmlMessageEntities.Parse("текст <blink>мигает</blink>", out _)
        );

        Assert.Contains("blink", error.Message);
    }

    [Fact]
    public void Незакрытый_Тег_Отклоняется()
    {
        var error = Assert.Throws<FormatException>(() =>
            HtmlMessageEntities.Parse("текст <b>жирный", out _)
        );

        Assert.Contains("b", error.Message);
    }

    [Fact]
    public void Закрывающий_Без_Открывающего_Отклоняется()
    {
        Assert.Throws<FormatException>(() => HtmlMessageEntities.Parse("текст </b>", out _));
    }

    /// <summary>
    /// Ссылка без адреса бессмысленна: метку показать можно, а открыть нечего.
    /// </summary>
    [Fact]
    public void Ссылка_Без_Адреса_Отклоняется()
    {
        Assert.Throws<FormatException>(() => HtmlMessageEntities.Parse("<a>метка</a>", out _));
    }

    /// <summary>
    /// Текст без разметки разбирается в пустой набор сущностей: пустой массив, а не
    /// <c>null</c> — иначе отправка отличалась бы от обычной.
    /// </summary>
    [Fact]
    public void Текст_Без_Разметки_Даёт_Пустой_Набор()
    {
        var text = HtmlMessageEntities.Parse("обычное сообщение", out var entities);

        Assert.Equal("обычное сообщение", text);
        Assert.Empty(entities);
    }

    /// <summary>
    /// Символы «меньше» и «больше» в обычном тексте не считаются разметкой: без
    /// этого сообщение вида <c>a &lt; b</c> разбиралось бы как незакрытый тег.
    /// </summary>
    [Fact]
    public void Закрывающая_Скобка_В_Тексте_Не_Ломает_Разбор()
    {
        var text = HtmlMessageEntities.Parse("a < b и c > d", out var entities);

        Assert.Equal("a < b и c > d", text);
        Assert.Empty(entities);
    }
}
