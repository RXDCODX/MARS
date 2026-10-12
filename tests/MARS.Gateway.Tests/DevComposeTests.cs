using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Команды запуска и тома сервисов в <c>docker-compose.dev.yml</c>.
/// </summary>
/// <remarks>
/// Stage <c>dev</c> в каждом Dockerfile объявлен как <c>FROM … AS dev</c>, а не
/// поверх <c>final</c>, поэтому <c>ENTRYPOINT ["dotnet", "MARS.X.dll"]</c> из
/// финального stage в него не попадает: у образа нет ни ENTRYPOINT, ни CMD, и
/// compose выполняет <c>command</c> как есть. Из этого следует и требование к
/// <c>dotnet</c> в начале команды, и требование собственных <c>obj</c>/<c>bin</c>:
/// без томов шестнадцать <c>dotnet watch</c> собирают общий <c>MARS.Shared</c>
/// в одни и те же файлы и блокируют их друг другу.
/// </remarks>
public partial class DevComposeTests
{
    /// <summary>
    /// Каждая команда dev-оверрайда обязана начинаться с <c>dotnet</c>.
    /// </summary>
    /// <remarks>
    /// Команда обязана звать <c>dotnet</c> явно и не начинаться с <c>watch</c> или
    /// <c>run</c>. Без <c>dotnet</c> первым <c>command: ["watch", …]</c>
    /// запускает <c>/usr/bin/watch</c> из procps: в образе SDK он стоит в
    /// <c>PATH</c> раньше dotnet, и контейнер уходит в рестарт с
    /// <c>watch: unrecognized option '--project'</c>.
    /// <para>
    /// Форма <c>sh -c "…"</c> тоже допускается: так запускается Gateway, где
    /// команда собирает приложение и запускает готовую сборку. Требование к
    /// <c>dotnet</c> остаётся тем же — внутри такой строки он есть.
    /// </para>
    /// </remarks>
    [Fact]
    public void DevКомандыЗапускаютсяЧерезDotnet()
    {
        var services = AppServices();

        Assert.True(services.Count > 0, "В docker-compose.dev.yml не нашлось ни одного сервиса.");

        foreach (var (name, block) in services)
        {
            var command = Commands(block);
            var verb = Regex.Match(command, "\\[\"(?<verb>[^\"]*)\"").Groups["verb"].Value;
            var bare = verb is "watch" or "run";
            var throughDotnet =
                verb == "dotnet" || command.Contains("dotnet ", StringComparison.Ordinal);

            Assert.True(
                throughDotnet && !bare,
                bare
                    ? $"Команда сервиса {name} начинается с \"{verb}\", поэтому compose ищет этот"
                        + " бинарь в PATH образа, а не в самом dotnet:"
                        + Environment.NewLine
                        + "  "
                        + command
                    : $"Команда сервиса {name} не зовёт dotnet:"
                        + Environment.NewLine
                        + "  "
                        + command
            );
        }
    }

    /// <summary>
    /// У каждого dev-сервиса обязаны быть свои <c>obj</c> и <c>bin</c>.
    /// </summary>
    /// <remarks>
    /// Репозиторий смонтирован во все контейнеры, а MSBuild пишет вывод проекта
    /// рядом с исходниками. Без тома на <c>obj</c>/<c>bin</c> общий
    /// <c>MARS.Shared</c> собирается одновременно в одни и те же файлы, сборка
    /// падает с «The process cannot access the file … because it is being used
    /// by another process», и стенд не поднимается.
    /// <para>
    /// Побочная выгода тома: собранные на Windows хостовые <c>obj</c> и
    /// <c>bin</c> больше не видны контейнерам. Раньше
    /// <c>project.assets.json</c> приезжал из хоста вместе с fallback-папкой
    /// <c>C:\Program Files (x86)\…\NuGetPackages</c>, которой в контейнере нет.
    /// </para>
    /// </remarks>
    [Fact]
    public void DevСервисыИзолируютObjИBin()
    {
        var services = AppServices();

        Assert.True(services.Count > 0, "В docker-compose.dev.yml не нашлось ни одного сервиса.");

        foreach (var (name, block) in services)
        {
            var mounts = Mounts(block);

            Assert.True(
                mounts.Any(mount => mount.EndsWith("/obj", StringComparison.Ordinal))
                    && mounts.Any(mount => mount.EndsWith("/bin", StringComparison.Ordinal)),
                $"У сервиса {name} нет своих obj и bin, поэтому его сборка делит файлы"
                    + " с остальными контейнерами:"
                    + Environment.NewLine
                    + "  "
                    + string.Join(Environment.NewLine + "  ", mounts)
            );
        }
    }

    /// <summary>
    /// Блоки-публикации задают только порты и ничего больше.
    /// </summary>
    /// <remarks>
    /// Инфраструктура и Shikimori выходят на хост только в dev-стенде, поэтому
    /// их публикации живут в этом файле, а не в <c>docker-compose.yml</c>. Блок с
    /// командой или томом в этом списке означал бы, что сервис не просто
    /// опубликовал порт, а поменял поведение — и <c>dotnet watch</c> у postgres
    /// этому первый признак.
    /// </remarks>
    [Fact]
    public void ПубликующиеБлокиЗадаютТолькоПорты()
    {
        var publishing = AllServices()
            .Where(entry => !entry.Block.Contains("*dev-defaults", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            publishing.Count > 0,
            "В docker-compose.dev.yml нет ни одного блока с публикацией портов."
        );

        foreach (var (name, block) in publishing)
        {
            var keys = block
                .Split('\n')
                .Skip(1)
                .Select(line => line.Trim())
                .Where(line =>
                    line.Length > 0
                    && line[0] != '#'
                    && !line.StartsWith("- ", StringComparison.Ordinal)
                )
                .ToList();

            Assert.True(
                keys.Count == 1 && keys[0] == "ports:",
                $"Блок сервиса {name} в dev-оверрайде задаёт не только публикацию портов:"
                    + Environment.NewLine
                    + block
            );
        }
    }

    /// <summary>
    /// Сервисы, которые dev-оверрайд собирает и запускает с hot-reload.
    /// </summary>
    /// <remarks>
    /// Признак — якорь <c>*dev-defaults</c>: он задаёт команду и цель сборки
    /// <c>dev</c>. Блоки-публикации (инфраструктура и Shikimori) добавляют только
    /// <c>ports</c>; требовать от них <c>dotnet watch</c> и своих <c>obj</c>/<c>bin</c>
    /// значило бы требовать этого от postgres.
    /// </remarks>
    private static List<(string Name, string Block)> AppServices() =>
        [
            .. AllServices()
                .Where(entry => entry.Block.Contains("*dev-defaults", StringComparison.Ordinal)),
        ];

    /// <summary>Сервисы оверрайда вместе с текстом их блоков.</summary>
    /// <remarks>
    /// Обход ограничен секцией <c>services</c> верхнего уровня. Без ограничения
    /// подошли бы и <c>build:</c> внутри якоря <c>x-dev-defaults</c>, и
    /// <c>nuget-packages:</c> из тома: у всех ключей внутри секции отступ ровно
    /// два пробела, как у настоящего сервиса.
    /// </remarks>
    private static List<(string Name, string Block)> AllServices()
    {
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.dev.yml");
        var lines = File.ReadAllLines(path);
        var section = Array.FindIndex(lines, line => line == "services:");

        Assert.True(section >= 0, "В docker-compose.dev.yml нет секции services.");

        var starts = new List<(int Line, string Name)>();
        var end = lines.Length;

        for (var index = section + 1; index < lines.Length; index++)
        {
            // Комментарий на нулевом отступе — не конец секции, а обычный
            // комментарий: YAML его не считает структурой. До этой оговорки
            // обход обрывался на первом же комментарии в начале строки (в блоке
            // gateway он один занимает двадцать пять строк), поэтому в списке
            // сервисов оставался один Gateway, и обе проверки dev-оверрайда
            // проходили, не посмотрев ни на один настоящий сервис.
            if (IsSectionEnd(lines[index]))
            {
                end = index;
                break;
            }

            var match = ServiceStart().Match(lines[index]);
            if (match.Success)
            {
                starts.Add((index, match.Groups["name"].Value));
            }
        }

        var services = new List<(string, string)>();

        for (var service = 0; service < starts.Count; service++)
        {
            // Последний блок обрывается на конце секции, а не на конце файла:
            // иначе в него попала бы секция `volumes` со всеми томами, и проверка
            // «только публикация» увидела бы у последнего сервиса чужие ключи.
            var next = service + 1 < starts.Count ? starts[service + 1].Line : end;

            services.Add(
                (starts[service].Name, string.Join('\n', lines[starts[service].Line..next]))
            );
        }

        return services;
    }

    /// <summary>Значение ключа <c>command</c> блока сервиса.</summary>
    private static string Commands(string block)
    {
        var match = Command().Match(block);

        Assert.True(
            match.Success,
            "У сервиса нет flow-команды одной строкой: " + Environment.NewLine + block
        );

        return match.Groups["value"].Value;
    }

    /// <summary>Точки монтирования блока: список из ключа <c>volumes</c>.</summary>
    private static List<string> Mounts(string block)
    {
        var lines = block.Split('\n');
        var start = Array.FindIndex(lines, line => VolumesKey().IsMatch(line));

        if (start < 0)
        {
            return [];
        }

        var mounts = new List<string>();

        for (var index = start + 1; index < lines.Length; index++)
        {
            var match = Mount().Match(lines[index]);

            if (match.Success)
            {
                mounts.Add(match.Groups["target"].Value);
            }
            else if (!string.IsNullOrWhiteSpace(lines[index]) && !IsListItem(lines[index]))
            {
                break;
            }
        }

        return mounts;
    }

    /// <summary>
    /// Строка заканчивает секцию <c>services</c> верхнего уровня.
    /// </summary>
    /// <remarks>
    /// Заканчивает её ключ верхнего уровня (<c>volumes:</c>, <c>networks:</c>), а
    /// комментарий без отступа — не заканчивает: YAML комментарии структурой не
    /// считает, и обрыв на них прятал от проверок все сервисы, кроме первого.
    /// </remarks>
    private static bool IsSectionEnd(string line) =>
        !string.IsNullOrWhiteSpace(line) && !line.StartsWith(' ') && line.TrimStart()[0] != '#';

    private static bool IsListItem(string line) => line.TrimStart().StartsWith('-');

    [GeneratedRegex("^  (?<name>[A-Za-z0-9_.-]+):\\s*$")]
    private static partial Regex ServiceStart();

    [GeneratedRegex("^\\s*command:\\s*(?<value>\\[.+\\])\\s*$", RegexOptions.Multiline)]
    private static partial Regex Command();

    [GeneratedRegex("^\\s*volumes:\\s*$")]
    private static partial Regex VolumesKey();

    /// <summary>Элемент списка томов: путь внутри контейнера, то есть часть после двоеточия.</summary>
    /// <remarks>
    /// Источник у dev-томов named (`dev-tts-obj:/src/…`) либо bind (`./:/src`), а
    /// проверяется цель: именно она отличает изолированный obj контейнера от
    /// общей папки исходников.
    /// </remarks>
    [GeneratedRegex("^\\s*-\\s*\\S+:(?<target>/\\S+)\\s*$")]
    private static partial Regex Mount();
}
