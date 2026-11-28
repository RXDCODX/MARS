using System.Text;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Разбиение длинного сообщения на части, умещающиеся в ограничение Telegram
/// (4096 символов в координатах UTF-16, то есть <see cref="string.Length"/>).
/// </summary>
/// <remarks>
/// <para>
/// Telegram отвергает сообщения длиннее 4096 символов с <c>400 MESSAGE_TOO_LONG</c>,
/// и до этого отправитель просто терял сообщение. Этот класс гарантирует, что
/// каждая часть не превышает лимит, а хвост, не влезший в заданное число частей,
/// явно обрезается с пометкой — вместо тихой потери.
/// </para>
/// <para>
/// Разметка (теги <c>b</c>, <c>i</c>, <c>code</c>, <c>pre</c>, <c>a</c>) режется
/// только в безопасных точках: вне синтаксиса тега и при нулевой глубине
/// открытых тегов, чтобы каждая часть оставалась корректной разметкой и
/// разбиралась <see cref="HtmlMessageEntities.Parse"/> независимо.
/// </para>
/// </remarks>
public static class MessageChunker
{
    /// <summary>Ограничение Telegram на длину текста сообщения (символы UTF-16).</summary>
    public const int MaxMessageLength = 4096;

    /// <summary>Максимальное число частей по умолчанию.</summary>
    public const int DefaultMaxChunks = 3;

    /// <summary>
    /// Разбивает HTML-разметку на части.
    /// </summary>
    public static ChunkResult SplitHtml(string html, int maxChunks = DefaultMaxChunks) =>
        Split(html ?? string.Empty, maxChunks, htmlMode: true);

    /// <summary>
    /// Разбивает обычный текст на части (без разметки).
    /// </summary>
    public static ChunkResult SplitPlain(string text, int maxChunks = DefaultMaxChunks) =>
        Split(text ?? string.Empty, maxChunks, htmlMode: false);

    private static ChunkResult Split(string text, int maxChunks, bool htmlMode)
    {
        if (maxChunks < 1)
        {
            maxChunks = 1;
        }

        if (string.IsNullOrEmpty(text) || text.Length <= MaxMessageLength)
        {
            return new ChunkResult([text ?? string.Empty], 0);
        }

        // Резерв под префикс «[i/n] » (максимум 8 символов при 3 частях)
        // и под пометку обрезки в последней части.
        const int prefixReserve = 8;
        const int markerReserve = 40;
        var budget = MaxMessageLength - prefixReserve;

        var chunks = new List<string>();
        var remaining = text;
        var consumed = 0;

        for (var i = 0; i < maxChunks && remaining.Length > 0; i++)
        {
            var isLast = i == maxChunks - 1;

            if (isLast)
            {
                if (remaining.Length <= budget)
                {
                    chunks.Add(remaining);
                    consumed += remaining.Length;
                    remaining = string.Empty;
                }
                else
                {
                    var cutBudget = budget - markerReserve;
                    var cut = FindSafeCut(remaining, cutBudget, htmlMode);
                    if (cut <= 0)
                    {
                        cut = Math.Min(cutBudget, remaining.Length);
                    }

                    if (cut > remaining.Length)
                    {
                        cut = remaining.Length;
                    }

                    if (cut == 0 && remaining.Length > 0)
                    {
                        cut = Math.Min(budget, remaining.Length);
                    }

                    var chunkContent = remaining.Substring(0, cut);
                    var suppressed = remaining.Length - cut;
                    var marker = $"\n…(+{suppressed} символов скрыто)";
                    chunks.Add(chunkContent + marker);
                    consumed += cut;
                    remaining = string.Empty;
                }

                break;
            }

            if (remaining.Length <= budget)
            {
                chunks.Add(remaining);
                consumed += remaining.Length;
                remaining = string.Empty;
                break;
            }

            var splitAt = FindSafeCut(remaining, budget, htmlMode);
            if (splitAt <= 0)
            {
                splitAt = Math.Min(budget, remaining.Length);
            }

            if (splitAt > remaining.Length)
            {
                splitAt = remaining.Length;
            }

            if (splitAt == 0 && remaining.Length > 0)
            {
                splitAt = Math.Min(budget, remaining.Length);
            }

            chunks.Add(remaining.Substring(0, splitAt));
            consumed += splitAt;
            remaining = remaining.Substring(splitAt);
        }

        // Если цикл завершился, а хвост остался (не должно происходить при isLast),
        // обрезаем его с пометкой.
        if (remaining.Length > 0)
        {
            var suppressed = remaining.Length;
            if (chunks.Count > 0)
            {
                chunks[^1] = chunks[^1] + $"\n…(+{suppressed} символов скрыто)";
            }
            else
            {
                chunks.Add("\n…(+" + suppressed + " символов скрыто)");
            }

            remaining = string.Empty;
        }

        var totalSuppressed = text.Length - consumed;
        if (totalSuppressed < 0)
        {
            totalSuppressed = 0;
        }

        if (chunks.Count > 1)
        {
            var total = chunks.Count;
            for (var i = 0; i < total; i++)
            {
                chunks[i] = $"[{i + 1}/{total}] " + chunks[i];
            }
        }

        return new ChunkResult(chunks.ToArray(), totalSuppressed);
    }

    /// <summary>
    /// Ищет безопасную позицию разреза (индекс символа, с которого начинается
    /// следующая часть). Возвращает 0, если безопасной точки нет.
    /// </summary>
    private static int FindSafeCut(string text, int budget, bool htmlMode)
    {
        if (budget >= text.Length)
        {
            return text.Length;
        }

        if (budget <= 0)
        {
            return 0;
        }

        if (!htmlMode)
        {
            var limit = Math.Min(budget - 1, text.Length - 1);
            var newline = text.LastIndexOf('\n', limit);
            if (newline >= 0 && newline + 1 > budget / 2)
            {
                return newline + 1;
            }

            var space = text.LastIndexOf(' ', limit);
            if (space >= 0 && space + 1 > budget / 4)
            {
                return space + 1;
            }

            return budget;
        }

        var formatDepth = 0;
        var lastSafeNewline = -1;
        var lastSafeSpace = -1;
        var lastSafeAny = -1;

        var i = 0;
        while (i < budget && i < text.Length)
        {
            var c = text[i];
            if (c == '<' && i + 1 < text.Length && (char.IsLetter(text[i + 1]) || text[i + 1] == '/'))
            {
                var closing = text[i + 1] == '/';
                var close = text.IndexOf('>', i);
                if (close < 0)
                {
                    break;
                }

                if (closing)
                {
                    formatDepth = Math.Max(0, formatDepth - 1);
                }
                else
                {
                    formatDepth++;
                }

                i = close + 1;
                continue;
            }

            if (formatDepth == 0)
            {
                if (c == '\n')
                {
                    lastSafeNewline = i + 1;
                }
                else if (c == ' ')
                {
                    lastSafeSpace = i + 1;
                }

                lastSafeAny = i + 1;
            }

            i++;
        }

        if (lastSafeNewline > budget / 2)
        {
            return lastSafeNewline;
        }

        if (lastSafeSpace > budget / 4)
        {
            return lastSafeSpace;
        }

        if (lastSafeAny > 0)
        {
            return lastSafeAny;
        }

        return 0;
    }
}

/// <summary>
/// Результат разбиения сообщения на части.
/// </summary>
public readonly record struct ChunkResult(string[] Chunks, int SuppressedChars);
