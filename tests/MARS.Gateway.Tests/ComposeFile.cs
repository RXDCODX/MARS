using System.Text;
using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Чтение блоков сервисов из <c>docker-compose.yml</c> как текста.
/// </summary>
/// <remarks>
/// Отдельный тип, а не пара методов внутри одного тест-класса, потому что блок
/// сервиса нужен двум наборам проверок: готовности инфраструктуры
/// (<see cref="ComposeReadinessTests"/>) и договорённостей стека matoi
/// (<see cref="MatoiStackTests"/>). Разбор нетривиален — блок определяется по
/// отступу, а значение healthcheck может идти по строкам, — и вторая копия
/// разбора разъехалась бы с первой правкой одного из них.
/// </remarks>
internal static partial class ComposeFile
{
    /// <summary>
    /// Блок сервиса из <c>docker-compose.yml</c>.
    /// </summary>
    /// <remarks>
    /// Следующий сервис начинается строкой вида <c>  имя:</c> — две позиции
    /// отступа, слово, двоеточие. Вложенные ключи уходят глубже, а комментарии
    /// начинаются с <c>#</c> и ключами не являются: без этой оговорки блок
    /// сервиса наползал бы на следующий и проверка проходила бы не по тому тексту.
    /// </remarks>
    public static string Block(string service)
    {
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml");
        var lines = File.ReadAllText(path).Split('\n');

        var start = Array.FindIndex(lines, line => line.TrimEnd('\r') == $"  {service}:");

        Assert.True(start >= 0, $"В docker-compose.yml нет сервиса {service}.");

        var block = new List<string>();

        for (var index = start + 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');

            if (ServiceStart().IsMatch(line))
            {
                break;
            }

            block.Add(line);
        }

        return string.Join(Environment.NewLine, block);
    }

    /// <summary>Значение ключа <c>test</c> в healthcheck.</summary>
    /// <remarks>
    /// Значение — flow-последовательность, и разбивать его по строкам законно, так
    /// что чтение одной строки проверяло бы форматирование, а не смысл. Поэтому
    /// строки склеиваются, пока не раскроются скобки.
    /// </remarks>
    public static string HealthcheckTest(string block)
    {
        var lines = block.Split('\n');
        var start = Array.FindIndex(lines, line => TestKeyLine().Match(line).Success);

        Assert.True(
            start >= 0,
            "У сервиса нет healthcheck с ключом test, поэтому compose считает его"
                + " здоровым без всякой проверки."
        );

        var value = new StringBuilder(TestKeyLine().Match(lines[start]).Groups["value"].Value);
        var index = start;

        // Читаем, пока скобки не сойдутся. Условие в двух частях, а не одно
        // «пока не сойдутся»: у значения, разбитого по строкам, на первой строке
        // пусто, а пустое сбалансировано по скобкам, и цикл вышел бы, не
        // прочитав ничего.
        while (index + 1 < lines.Length && (Unclosed(value.ToString()) > 0 || IsBlank(value)))
        {
            index++;
            value.Append(' ').Append(lines[index].Trim());
        }

        return value.ToString().Trim();
    }

    private static bool IsBlank(StringBuilder value) => value.ToString().Trim().Length == 0;

    /// <summary>Сколько потоков ещё не закрыто: отрицательное — лишняя закрывающая.</summary>
    private static int Unclosed(string text)
    {
        var opened = 0;

        foreach (var character in text)
        {
            if (character == '[')
            {
                opened++;
            }
            else if (character == ']')
            {
                opened--;
            }
        }

        return opened;
    }

    [GeneratedRegex("^  [A-Za-z0-9_.-]+:\\s*$")]
    private static partial Regex ServiceStart();

    /// <summary>Ключ <c>test</c> с возможно пустым значением: поток может идти</summary>
    /// <remarks>
    /// следующими строками. Значение не может быть обязательным и не может
    /// захватывать <c>\r</c>: в файле переводы строк CRLF, и <c>.</c> в .NET
    /// совпадает и с <c>\r</c>, из-за чего значение читалось как одна
    /// переводная строка, то есть как пустое.
    /// </remarks>
    [GeneratedRegex("^\\s*test:\\s*(?<value>[^\\r\\n]*)")]
    private static partial Regex TestKeyLine();
}
