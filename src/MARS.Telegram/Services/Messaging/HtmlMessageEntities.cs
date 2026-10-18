using System.Text;
using TL;

namespace MARS.Telegram.Services.Messaging;

/// <summary>
/// Разбор разметки Telegram в сущности сообщения.
/// </summary>
/// <remarks>
/// <para>
/// У Bot API был <c>ParseMode.Html</c>: сервис отдавал строку с тегами, а
/// разбирал её Telegram. У WTelegram такого нет — <c>SendMessageAsync</c>
/// принимает готовый массив <c>MessageEntity</c>, и разбирать разметку нужно
/// здесь. Без этого <c>&lt;b&gt;</c> ушёл бы в чат как текст.
/// </para>
/// <para>
/// Поддерживается подмножество, которым сервис пользуется: <c>b</c>, <c>i</c>,
/// <c>code</c>, <c>pre</c> и <c>a</c>. Неизвестный тег — ошибка, а не «отправим
/// как есть»: молча уехавшая в чат сырая разметка хуже явного отказа, потому
/// что её замечают уже в проде.
/// </para>
/// </remarks>
public static class HtmlMessageEntities
{
    /// <summary>
    /// Возвращает текст без тегов и набор сущностей к нему.
    /// </summary>
    /// <param name="html">Исходная разметка.</param>
    /// <param name="text">Текст без тегов.</param>
    /// <param name="entities">Сущности: смещения — в координатах <paramref name="text"/>.</param>
    /// <exception cref="FormatException">Разметка не закрыта или содержит неизвестный тег.</exception>
    public static string Parse(string html, out MessageEntity[] entities)
    {
        var parsed = new List<MessageEntity>();
        var text = new StringBuilder();
        var cursor = 0;
        var closing = new Stack<OpenedTag>();

        while (cursor < html.Length)
        {
            var open = html.IndexOf('<', cursor);

            if (open < 0 || !IsTagStart(html, open))
            {
                // «<» в обычном тексте — не тег: сообщение «a < b» не должно
                // разбираться как незакрытый тег. Началом тега считается только
                // «<» вплотную к букве или к закрывающей косой черте.
                text.Append(open < 0 ? html[cursor..] : html[cursor..(open + 1)]);
                cursor = open < 0 ? html.Length : open + 1;
                continue;
            }

            var close = html.IndexOf('>', open);

            if (close < 0)
            {
                throw new FormatException($"Разметка не закрыта: {html[open..]}");
            }

            text.Append(html[cursor..open]);
            var tag = html[(open + 1)..close].Trim();
            // У закрывающего тега имя лежит после косой черты, а у открывающего —
            // первым словом, потому что дальше идут атрибуты. Имя нужно отдельно:
            // сравнение тега целиком с «a» не сошлось бы из-за href.
            var isClosing = tag.StartsWith('/');
            var name = NameOf(isClosing ? tag[1..].Trim() : tag);
            var offset = text.Length;

            if (isClosing)
            {
                if (closing.Count == 0 || closing.Peek().Tag != name)
                {
                    throw new FormatException($"Закрывающий тег </{name}> не открыт");
                }

                var opened = closing.Pop();
                parsed.Add(Entity(opened, text.Length - opened.Offset));
            }
            else if (Known(name))
            {
                closing.Push(new OpenedTag(name, offset, UrlOf(tag)));
            }
            else
            {
                throw new FormatException($"Неизвестный тег <{name}>");
            }

            cursor = close + 1;
        }

        if (closing.Count > 0)
        {
            throw new FormatException($"Не закрыт тег <{closing.Peek().Tag}>");
        }

        entities = [.. parsed];
        return text.ToString();
    }

    /// <summary>Имя тега без атрибутов.</summary>
    private static string NameOf(string tag)
    {
        var space = tag.IndexOf(' ');

        return (space < 0 ? tag : tag[..space]).Trim();
    }

    /// <summary>
    /// Начинается ли здесь тег: «&lt;» должен стоять вплотную к букве или к
    /// закрывающей косой черте.
    /// </summary>
    private static bool IsTagStart(string html, int open)
    {
        var result = false;

        if (open + 1 < html.Length)
        {
            var next = html[open + 1];

            result = char.IsLetter(next) || next == '/';
        }

        return result;
    }

    /// <summary>Открытый тег: где начался и, для ссылки, куда ведёт.</summary>
    private readonly record struct OpenedTag(string Tag, int Offset, string? Url);

    /// <summary>
    /// Адрес из атрибута <c>href</c>. У ссылки без адреса сущность бессмысленна:
    /// Telegram покажет метку, но отправить её будет некуда.
    /// </summary>
    /// <remarks>
    /// Сравнение с именем, а не с тегом целиком: в теге ещё стоит атрибут, и
    /// сравнение не сошлось бы — адрес молча оказался бы пустым.
    /// </remarks>
    private static string? UrlOf(string tag)
    {
        if (!string.Equals(NameOf(tag), "a", StringComparison.Ordinal))
        {
            return null;
        }

        var href = tag.IndexOf("href", StringComparison.Ordinal);
        var open = href < 0 ? -1 : tag.IndexOf('=', href);
        var first = open < 0 ? -1 : tag.IndexOf('"', open);
        var second = first < 0 ? -1 : tag.IndexOf('"', first + 1);

        return first < 0 || second < 0 ? null : tag[(first + 1)..second];
    }

    /// <summary>
    /// Собирает сущность по открытому тегу. Типов ровно столько, сколько тегов мы
    /// понимаем: неизвестный тег — ошибка, а не «отправим как есть».
    /// </summary>
    private static MessageEntity Entity(OpenedTag opened, int length) =>
        opened.Tag switch
        {
            "b" => new MessageEntityBold { offset = opened.Offset, length = length },
            "i" => new MessageEntityItalic { offset = opened.Offset, length = length },
            "code" => new MessageEntityCode { offset = opened.Offset, length = length },
            "pre" => new MessageEntityPre
            {
                offset = opened.Offset,
                length = length,
                language = "",
            },
            "u" => new MessageEntityUnderline { offset = opened.Offset, length = length },
            "s" => new MessageEntityStrike { offset = opened.Offset, length = length },
            // Ссылка без адреса бессмысленна: метку показать можно, а открыть
            // нечего. Пустой адрес Telegram не примет, поэтому это тот же отказ.
            "a" when !string.IsNullOrWhiteSpace(opened.Url) => new MessageEntityTextUrl
            {
                offset = opened.Offset,
                length = length,
                url = opened.Url,
            },
            "a" => throw new FormatException("У ссылки <a> нет адреса: нужен href=\"...\""),
            _ => throw new FormatException($"Неизвестный тег <{opened.Tag}>"),
        };

    private static bool Known(string tag) =>
        tag is "b" or "i" or "code" or "pre" or "a" or "u" or "s";

    /// <summary>
    /// Ссылка с адресом: адрес берётся из атрибута, а сама метка — обычный текст.
    /// </summary>
    public static string Link(string label, string url) => $"<a href=\"{url}\">{label}</a>";
}
