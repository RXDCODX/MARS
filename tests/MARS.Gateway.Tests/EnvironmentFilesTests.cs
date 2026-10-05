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
/// скрипты и CI не ссылаются на удалённый <c>.env.example</c> и передают compose
/// <c>--env-file</c>: без флага compose возьмёт дефолтный <c>.env</c>, которого
/// в репозитории уже нет, и стенд поднимется на пустых значениях по умолчанию.
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
    /// Ключи, которые в боевом шаблоне обязаны быть пустыми: значение, попавшее в
    /// репозиторий, — это пароль или ключ, доступный всем, кто читает репозиторий.
    /// </summary>
    private static readonly string[] SecretKeys =
    [
        "POSTGRES_PASSWORD",
        "MARS_TWITCH_PASSWORD",
        "MARS_WAIFU_PASSWORD",
        "MARS_CHAT_PASSWORD",
        "MARS_MEDIA_PASSWORD",
        "MARS_SCOREBOARD_PASSWORD",
        "MARS_CINEMA_PASSWORD",
        "MARS_MEDIASTORAGE_PASSWORD",
        "MARS_SHIKIMORI_PASSWORD",
        "MARS_ADMIN_PASSWORD",
        "MARS_ALERTS_PASSWORD",
        "MARS_VIDEOS365_PASSWORD",
        "RABBITMQ_PASSWORD",
        "GRAFANA_PASSWORD",
        "MATOI_REDIS_PASSWORD",
        "MATOI_API_KEY",
        "SERVICE_API_KEY",
    ];

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

        var filled = SecretKeys
            .Where(key =>
                values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            )
            .Select(key => $"  {key}={values[key]}")
            .ToList();

        var absent = SecretKeys.Where(key => !values.ContainsKey(key)).ToList();

        var report = string.Join(
            "\n",
            filled.Select(line => "  значение задано в репозитории: " + line),
            absent.Select(key => $"  ключа нет вовсе: {key}")
        );

        Assert.True(
            filled.Count == 0 && absent.Count == 0,
            "В боевом шаблоне секреты обязаны быть пустыми:\n" + report
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
    /// Скрипты не помнят удалённый шаблон и называют нужное окружение.
    /// </summary>
    /// <remarks>
    /// Ссылка на <c>.env.example</c> выглядит безобидно и стоит в комментарии
    /// скрипта, но её читают как инструкцию: скопировать нечего, и это выглядит
    /// как «файл есть». Стенд выбирает оба профиля (переключатель <c>-Dev</c>),
    /// а e2e поднимает боевой стенд целиком и потому обязан называть
    /// <c>.env.production</c> явно.
    /// </remarks>
    [Fact]
    public void СкриптыНазываютНужноеОкружение()
    {
        foreach (var script in StackScripts)
        {
            var text = File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(script));

            Assert.DoesNotContain(".env.example", text, StringComparison.Ordinal);

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
        }
    }

    /// <summary>
    /// Ни compose, ни CI не ссылаются на удалённый шаблон.
    /// </summary>
    /// <remarks>
    /// Ссылки выглядят безобидно и стоят в комментариях, но их читают как
    /// инструкцию: «скопируй <c>.env.example</c>» — и человек идёт искать файл,
    /// которого нет.
    /// </remarks>
    [Fact]
    public void УдалённыйШаблонНигдеНеУпоминается()
    {
        var files = new List<string>
        {
            "docker-compose.yml",
            "docker-compose.dev.yml",
            ".github/workflows/ci.yml",
            "README.md",
        };

        var stale = files
            .Select(file =>
                (
                    File: file,
                    Text: File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(file))
                )
            )
            .Where(pair => pair.Text.Contains(".env.example", StringComparison.Ordinal))
            .Select(pair => "  " + pair.File)
            .ToList();

        Assert.True(
            stale.Count == 0,
            "Файлы всё ещё ссылаются на удалённый .env.example:\n" + string.Join("\n", stale)
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
