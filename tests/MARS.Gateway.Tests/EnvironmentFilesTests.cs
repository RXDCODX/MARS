using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MARS.Gateway.Tests;

/// <summary>
/// Договорённости двух файлов окружения: <c>.env.development</c> и
/// <c>.env.production</c> с шаблонами рядом.
/// </summary>
/// <remarks>
/// <para>
/// Раньше окружение было одно: <c>.env</c>, который compose читал сам, плюс
/// <c>.env.example</c> в репозитории. Назвать его «одним файлом» было можно
/// только пока стенд был единственным. Теперь сред две, и каждая соответствует
/// своему <c>ASPNETCORE_ENVIRONMENT</c>: <c>docker-compose.dev.yml</c> ставит
/// <c>Development</c> (сервисы читают <c>appsettings.Development.json</c>), а
/// <c>docker-compose.yml</c> — <c>Production</c>.
/// </para>
/// <para>
/// Что проверяется и почему — каждая проверка закрывает способ, которым такой
/// разъезд происходит молча:
/// </para>
/// <list type="bullet">
/// <item>
/// набор ключей в двух шаблонах совпадает: переменная, добавленная в один шаблон,
/// не появится в боевом стенде, и compose отдаст <c>:-</c>-дефолт — то есть
/// сервис поднимется с пустым паролем или без токена;
/// </item>
/// <item>
/// каждая <c>${ПЕРЕМЕННАЯ}</c> из compose есть в обоих шаблонах: compose
/// подставляет значения из файла окружения, а не из воздуха;
/// </item>
/// <item>
/// боевой шаблон не повторяет dev-значения секретов: шаблон лежит в
/// репозитории, и dev-пароль, оставшийся в боевом шаблоне, — это не «удобно»,
/// а пароль стенда на боевом хосте;
/// </item>
/// <item>
/// реальные файлы окружений игнорируются git, а шаблоны — нет: правило на
/// <c>.env.*</c> должно быть широким, иначе новое окружение уедет в коммит
/// целиком, вместе с токенами;
/// </item>
/// <item>
/// <c>.dockerignore</c> не пускает файлы окружений в контекст сборки: паттерн
/// <c>**/.env</c> ловит ровно один файл, а не <c>.env.development</c>;
/// </item>
/// <item>
/// скрипты и CI передают compose <c>--env-file</c> и называют нужное окружение:
/// без флага compose возьмёт дефолтный <c>.env</c>, которого в репозитории уже
/// нет, и стенд поднимется на пустых значениях по умолчанию;
/// </item>
/// <item>
/// ключи не разъезжаются и с перечнем <c>.env.example</c>, который остаётся в
/// репозитории как список ключей: пока шаблонов сред нет, скрипты берут его
/// как откат, и незаметённая новая переменная разошлась бы с ним;
/// </item>
/// </list>
/// <para>
/// Файлы читаются через <see cref="ClientUiImageWorkflowTests.FindRepositoryFile"/>:
/// копировать их в выход теста не нужно, а путь вглубь от <c>bin/Release</c>
/// рассыпался бы при смене конфигурации сборки.
/// </para>
/// </remarks>
public partial class EnvironmentFilesTests
{
    /// <summary>Шаблоны окружений: имя файла — среда.</summary>
    private static readonly string[] Templates =
    [
        ".env.development.example",
        ".env.production.example",
    ];

    /// <summary>Реальные файлы окружений: имя файла — среда.</summary>
    private static readonly string[] RealFiles = [".env.development", ".env.production"];

    /// <summary>
    /// Скрипты, которые поднимают стенд и потому обязаны знать про файл окружения.
    /// </summary>
    private static readonly string[] StackScripts =
    [
        "scripts/windows/stack.ps1",
        "scripts/unix/stack.sh",
        "scripts/windows/common.ps1",
        "scripts/unix/common.sh",
        "scripts/windows/e2e.ps1",
        "scripts/unix/e2e.sh",
    ];

    /// <summary>
    /// Ключи, которые в боевом шаблоне обязаны быть пустыми — по имени, а не
    /// списком.
    /// </summary>
    /// <remarks>
    /// Список ключей в тесте протухает тихо: сервис добавляет
    /// <c>MATOI_TOKEN</c>, боевой шаблон забывают обнулить, проверка остаётся
    /// зелёной, потому что она сверяет не то, что важно. Правило по суффиксу
    /// ловит новый секрет без правки теста, а конкретные имена держатся в списке
    /// исключений: <c>SHIKIMORI_CLIENT_NAME</c> и <c>MEDIA_GIT_BRANCH</c> — не
    /// секреты, но стендовые значения их тоже не должны уезжать в репозиторий.
    /// </remarks>
    private static readonly string[] SecretSuffixes =
    [
        "PASSWORD",
        "TOKEN",
        "SECRET",
        "API_KEY",
        "CLIENT_ID",
        "OAUTH",
        "CONNECTION_STRING",
        "GIT_USERNAME",
    ];

    /// <summary>Ключи вне правила, которые всё равно обязаны быть пустыми.</summary>
    private static readonly string[] ExtraSecrets = ["SHIKIMORI_CLIENT_NAME", "MEDIA_GIT_BRANCH"];

    /// <summary>Считается ли ключ секретом.</summary>
    private static bool IsSecret(string key) =>
        ExtraSecrets.Contains(key, StringComparer.Ordinal)
        || SecretSuffixes.Any(suffix => key.EndsWith(suffix, StringComparison.Ordinal));

    /// <summary>
    /// Набор ключей в двух шаблонах обязан совпадать.
    /// </summary>
    /// <remarks>
    /// Расхождение не роняет сборку и не роняет стенд: <c>docker-compose.yml</c>
    /// подставляет <c>${ПЕРЕМЕННАЯ:-значение}</c>, и стенд поднимается — только с
    /// dev-паролем вместо боевого или вовсе без токена. Обнаруживается это на
    /// боевом хосте, то есть позже всего.
    /// </remarks>
    [Fact]
    public void НаборКлючейВШаблонахСовпадает()
    {
        var development = KeysOf(Templates[0]);
        var production = KeysOf(Templates[1]);

        var missingInProduction = development.Except(production, StringComparer.Ordinal).ToList();
        var missingInDevelopment = production.Except(development, StringComparer.Ordinal).ToList();

        var mismatch = string.Join(
            "\n",
            missingInProduction.Select(key => $"  отсутствует в {Templates[1]}: {key}"),
            missingInDevelopment.Select(key => $"  отсутствует в {Templates[0]}: {key}")
        );

        Assert.True(
            missingInProduction.Count == 0 && missingInDevelopment.Count == 0,
            "Наборы ключей в шаблонах окружений разошлись:\n" + mismatch
        );
    }

    /// <summary>
    /// Перечень ключей без среды не разошёлся с шаблонами.
    /// </summary>
    /// <remarks>
    /// <c>.env.example</c> остаётся в репозитории и остаётся источником стендовых
    /// значений, пока шаблонов сред нет. Значит новая переменная обязана появиться
    /// во всех трёх файлах, иначе скрипт создаст файл окружения из старого набора
    /// и стенд поднимется со значением по умолчанию.
    /// </remarks>
    [Fact]
    public void ПереченьКлючейСовпадаетСШаблонамиОкружений()
    {
        var reference = KeysOf(".env.example");

        Assert.NotEmpty(reference);

        foreach (var template in Templates)
        {
            var keys = KeysOf(template);
            var missing = reference.Except(keys, StringComparer.Ordinal).ToList();

            Assert.True(
                missing.Count == 0,
                $"В {template} нет ключей из .env.example: {string.Join(", ", missing)}."
                    + " Скрипт создаёт файл окружения из шаблона среды, а перечень"
                    + " ключей нужен и как откат: без него стенд молча поднимется"
                    + " со значением по умолчанию."
            );
        }
    }

    /// <summary>
    /// Каждая переменная, которую compose подставляет, есть в обоих шаблонах.
    /// </summary>
    /// <remarks>
    /// Состав переменных берётся из самого <c>docker-compose.yml</c>, а не из
    /// списка в тесте: список разошёлся бы с compose при первом же новом сервисе,
    /// и ровно его переменной в шаблонах не окажется.
    /// </remarks>
    [Fact]
    public void ВсеПеременныеComposeЕстьВШаблонах()
    {
        var compose = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml")
        );
        var referenced = ComposeVariable()
            .Matches(compose)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            referenced.Count > 0,
            "В docker-compose.yml не найдено ни одной ${ПЕРЕМЕННАЯ}."
        );

        foreach (var template in Templates)
        {
            var keys = KeysOf(template).ToHashSet(StringComparer.Ordinal);
            var missing = referenced.Where(key => !keys.Contains(key)).ToList();

            Assert.True(
                missing.Count == 0,
                $"В {template} нет переменных, которые подставляет docker-compose.yml: "
                    + string.Join(", ", missing)
                    + ". Compose возьмёт значение по умолчанию, и стенд поднимется не с тем."
            );
        }
    }

    /// <summary>
    /// Боевой шаблон не содержит dev-значений секретов.
    /// </summary>
    /// <remarks>
    /// Проверяется не «значение выглядит слабым», а конкретный долг: шаблон лежит
    /// в репозитории, и перенесённое в него <c>mars</c> — это пароль, который
    /// любой может подставить. Секрет в боевом шаблоне обязан быть пустым: его
    /// заполняют в <c>.env.production</c>, который в git не попадает.
    /// </remarks>
    [Fact]
    public void БоевойШаблонНеСодержитСекретов()
    {
        var values = ValuesOf(Templates[1]);

        var secrets = values
            .Keys.Where(IsSecret)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            secrets.Count > 0,
            "В боевом шаблоне не найдено ни одного ключа, похожего на секрет."
                + " Значит правило по суффиксу разъехалось с составом шаблона,"
                + " и проверка никого не защищает."
        );

        var filled = secrets
            .Where(key => !string.IsNullOrWhiteSpace(values[key]))
            .Select(key => $"  {key}={values[key]}")
            .ToList();

        Assert.True(
            filled.Count == 0,
            "В боевом шаблоне секреты обязаны быть пустыми — их заполняют в"
                + " .env.production, который в git не попадает:\n"
                + string.Join("\n", filled)
        );
    }

    /// <summary>
    /// Реальные файлы окружений игнорируются git, шаблоны — нет.
    /// </summary>
    /// <remarks>
    /// Проверяется текст правил, а не результат <c>git check-ignore</c>: правило
    /// обязано быть широким (<c>.env.*</c>), иначе новое окружение, добавленное без
    /// правки <c>.gitignore</c>, уедет в коммит целиком — вместе с токенами.
    /// </remarks>
    [Fact]
    public void GitРазличаетРеальныеФайлыИШаблоны()
    {
        var gitignore = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(".gitignore")
        );

        AssertRule(gitignore, ".env", "точный файл .env должен игнорироваться");
        AssertRule(gitignore, ".env.*", "все .env.<среда> должны игнорироваться широким правилом");
        AssertRule(
            gitignore,
            "!.env.*.example",
            "шаблоны .env.<среда>.example обязаны быть в репозитории"
        );
    }

    /// <summary>
    /// Файлы окружений не попадают в контекст сборки образа.
    /// </summary>
    /// <remarks>
    /// Старое правило <c>**/.env</c> ловит ровно файл <c>.env</c>. С двумя
    /// окружениями оно перестало работать, а токены в контексте сборки — это они в
    /// слое <c>COPY</c> и в кэше сборщика.
    /// </remarks>
    [Fact]
    public void DockerignoreИсключаетФайлыОкружений()
    {
        var dockerignore = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(".dockerignore")
        );

        AssertRule(dockerignore, "**/.env", "точный файл .env должен исключаться");
        AssertRule(
            dockerignore,
            "**/.env.*",
            "Файлы .env.development и .env.production должны исключаться: **/.env их не ловит"
        );
    }

    /// <summary>
    /// Файл окружения выбирается в общем коде скриптов — по тому же переключателю,
    /// что и compose-файл.
    /// </summary>
    /// <remarks>
    /// Выбор живёт в <c>common.*</c>, а не в каждом скрипте: иначе стенд, e2e и
    /// любой будущий сценарий подняли бы стенд на разных секретах, и разошлись
    /// бы молча. <c>docker-compose.dev.yml</c> ставит
    /// <c>ASPNETCORE_ENVIRONMENT=Development</c>, а <c>docker-compose.yml</c> —
    /// <c>Production</c>, поэтому <c>-Dev</c> выбирает и compose-файл, и файл
    /// окружения: разъехаться могут только два переключателя подряд, а их один.
    /// </remarks>
    [Fact]
    public void ОбщийКодВыбираетФайлОкруженияПоПрофилю()
    {
        var shared = new[] { "scripts/windows/common.ps1", "scripts/unix/common.sh" };

        foreach (var script in shared)
        {
            var text = File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(script));

            Assert.True(
                text.Contains("--env-file", StringComparison.Ordinal),
                $"{script} не передаёт compose --env-file: compose возьмёт дефолтный .env, которого нет."
            );

            foreach (var file in RealFiles)
            {
                Assert.True(
                    text.Contains(file, StringComparison.Ordinal),
                    $"{script} не упоминает {file}: профиль окружения в нём не выбирается."
                );
            }
        }
    }

    /// <summary>
    /// Скрипты стенда не строят файл окружения из перечня ключей без среды.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.env.example</c> в репозитории остаётся — это перечень ключей со
    /// значениями стенда, и пока шаблонов сред нет, скрипты берут его как
    /// откат. Но откат живёт в общем коде платформы (<c>common.*</c>), а не в
    /// <c>stack</c> и <c>e2e</c>: иначе стенд поднимался бы по файлу без среды
    /// то в одной среде, то в другой, и разошёлся бы с <c>-Dev</c> молча.
    /// </para>
    /// <para>
    /// Запрет на любое упоминание здесь был бы неправильным: он запретил бы
    /// документированный откат и заставил бы молчать предупреждение, из-за
    /// которого стенд не поднимается с чужими секретами.
    /// </para>
    /// </remarks>
    [Fact]
    public void СкриптыНазываютНужноеОкружение()
    {
        foreach (var script in StackScripts)
        {
            var text = File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(script));

            var required = script.Contains("e2e", StringComparison.Ordinal)
                ? [".env.production"]
                : RealFiles;

            foreach (var file in required)
            {
                Assert.True(
                    text.Contains(file, StringComparison.Ordinal),
                    $"{script} не упоминает {file}."
                );
            }

            var isSharedCode = script.Contains("common", StringComparison.Ordinal);

            if (!isSharedCode)
            {
                Assert.DoesNotContain(".env.example", text, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Ни compose, ни CI не велят копировать перечень ключей без среды.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.env.example</c> остаётся в репозитории, но инструкция «скопируйте его
    /// в <c>.env</c>» больше не работает: compose читает файл, переданный
    /// <c>--env-file</c>, и человек выполнит её, поднимет стенд и упрётся в
    /// пустые пароли или в не ту среду.
    /// </para>
    /// <para>
    /// Запрещается именно инструкция (<c>cp .env.example</c> и любая ссылка в
    /// compose и CI), а не само упоминание: в <c>README.md</c> файл остаётся как
    /// перечень ключей, и ссылка на него там обязана быть.
    /// </para>
    /// </remarks>
    [Fact]
    public void ComposeИCiНеВелятКопироватьШаблонБезСреды()
    {
        var files = new List<string>
        {
            "docker-compose.yml",
            "docker-compose.dev.yml",
            ".github/workflows/ci.yml",
        };

        var stale = files
            .Select(file =>
                (
                    File: file,
                    Text: File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(file))
                )
            )
            .Where(pair =>
                pair.Text.Contains(".env.example", StringComparison.Ordinal)
                || pair.Text.Contains("cp .env ", StringComparison.Ordinal)
            )
            .Select(pair => "  " + pair.File)
            .ToList();

        Assert.True(
            stale.Count == 0,
            "Compose и CI всё ещё ссылаются на шаблон без среды — инструкция не сработает:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, stale)
        );
    }

    /// <summary>Ключи окружения из файла: строки <c>КЛЮЧ=значение</c>.</summary>
    private static List<string> KeysOf(string file) => [.. Read(file).Keys];

    /// <summary>Значения окружения из файла: строки <c>КЛЮЧ=значение</c>.</summary>
    private static Dictionary<string, string> ValuesOf(string file) => Read(file);

    /// <summary>Читает файл окружения как словарь.</summary>
    /// <remarks>
    /// Комментарии и пустые строки пропускаются, значение по первому <c>=</c>:
    /// секрет с <c>=</c> внутри не должен распадаться на два ключа. Дубликат
    /// ключа — не ошибка этого разбора: последний выигрывает, ровно как и в
    /// docker compose.
    /// </remarks>
    private static Dictionary<string, string> Read(string file)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        var path = ClientUiImageWorkflowTests.FindRepositoryFile(file);

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed.Length > 0 && trimmed[0] != '#')
            {
                var separator = trimmed.IndexOf('=');

                if (separator > 0)
                {
                    result[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim();
                }
            }
        }

        return result;
    }

    /// <summary>Требует наличия правила среди строк <c>.gitignore</c>/<c>.dockerignore</c>.</summary>
    private static void AssertRule(string text, string rule, string message)
    {
        var present = text.Split('\n')
            .Select(line => line.Trim().TrimEnd('\r'))
            .Contains(rule, StringComparer.Ordinal);

        Assert.True(present, message + $" (ожидалась строка «{rule}»).");
    }

    [GeneratedRegex(@"\$\{(?<name>[A-Za-z0-9_]+)")]
    private static partial Regex ComposeVariable();
}
