using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Проверки публикации образа клиента в release-workflow.
/// </summary>
/// <remarks>
/// Ловушка здесь молчаливая. Матрица берёт путь к Dockerfile из
/// <c>matrix.project</c>, а имя образа — из <c>matrix.service</c>. Если бы строка
/// клиента не задавала путь явно, шаг сборки собрал бы
/// <c>src/MARS.Gateway/Dockerfile</c>, то есть .NET-образ шлюза, и опубликовал
/// его как <c>mars-client-ui</c>. Задача осталась бы зелёной, образ был бы в
/// registry, а стенд — сломанным на <c>client-ui</c>.
/// </remarks>
public partial class ClientUiImageWorkflowTests
{
    /// <summary>
    /// Ищет файл вверх по дереву от рабочего каталога теста. Путь вглубь от
    /// <c>bin/Release</c> копился бы пять раз и рассыпался бы при смене
    /// конфигурации сборки.
    /// </summary>
    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. segments]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Файл не найден вверх по дереву: {Path.Combine(segments)}"
        );
    }

    private static string ReadWorkflow() =>
        File.ReadAllText(FindRepositoryFile(".github", "workflows", "release-microservices.yml"));

    /// <summary>
    /// <summary>
    /// Ключ, съехавший в нулевой отступ, ломает workflow молча.
    /// </summary>
    /// <remarks>
    /// Проверено на живом пуше: правка матрицы сдвинула <c>matrix:</c> и
    /// <c>context:</c> в начало строки. YAML перестал быть отображением,
    /// GitHub не смог разобрать файл и упал с total_count: 0 jobs — без единой
    /// задачи и без внятной причины. Текстовые проверки выше этого не видят:
    /// они ищут подстроки, а сломанный файл читается прекрасно.
    ///
    /// Правило не «любой отступ больше нуля», а «в нулевом отступе допустимы
    /// только корневые ключи workflow». <c>matrix:</c> и <c>context:</c> в
    /// нулевом отступе означают, что ключ вырвался из своего блока — именно так
    /// это и выглядело.
    /// </remarks>
    [Theory]
    [InlineData("ci.yml")]
    [InlineData("auto-format.yml")]
    [InlineData("release-microservices.yml")]
    public void Workflow_yaml_keys_stay_in_their_blocks(string workflowFile)
    {
        var lines = File.ReadAllLines(FindRepositoryFile(".github", "workflows", workflowFile));

        string[] rootKeys =
        [
            "name",
            "on",
            "env",
            "jobs",
            "permissions",
            "concurrency",
            "defaults",
            "run-name",
        ];

        var offenders = new List<string>();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith('-'))
            {
                continue;
            }

            if (!LooksLikeKey(trimmed))
            {
                continue;
            }

            var indent = line.Length - trimmed.Length;
            var key = trimmed[..trimmed.IndexOf(':')].Trim();

            if (indent == 0 && !rootKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                offenders.Add($"{index + 1}: ключ '{key}' в нулевом отступе");
            }

            if (indent % 2 != 0)
            {
                offenders.Add($"{index + 1}: нечётный отступ ({indent}) у '{key}'");
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{workflowFile}: ключи вырвались из блоков или имеют нечётный отступ. "
                + "GitHub не разберёт workflow и упадёт без задач:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders)
        );
    }

    private static bool LooksLikeKey(string trimmed)
    {
        var colon = trimmed.IndexOf(':');

        if (colon <= 0)
        {
            return false;
        }

        var name = trimmed[..colon];

        // «name: run steps» — значение с двоеточием, ключ здесь только name.
        return !name.Contains(' ')
            || name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    /// <summary>
    /// Шаг сборки обязан уважать <c>matrix.dockerfile</c>. Пока выражение жёстко
    /// содержит <c>/Dockerfile</c>, клиент собрал бы образ шлюза.
    /// </summary>
    [Fact]
    public void Build_step_honours_per_row_dockerfile()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("matrix.dockerfile", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_ui_row_exists_and_points_at_its_own_dockerfile()
    {
        var workflow = ReadWorkflow();

        // Строка матрицы: - service: client-ui, затем project и dockerfile.
        Assert.Matches(ClientUiRow(), workflow);
    }

    /// <summary>
    /// Dockerfile и nginx-конфиг обязаны существовать по путям, которые
    /// использует сборка. Иначе падение пришлось бы на релиз, а не на сборку.
    /// </summary>
    [Theory]
    [InlineData("src", "MARS.Gateway", "ClientApp.Dockerfile")]
    [InlineData("infrastructure", "nginx", "client-ui.conf")]
    public void Build_inputs_exist_in_repository(params string[] segments)
    {
        var path = FindRepositoryFile(segments);

        Assert.True(File.Exists(path), $"Файл не найден: {path}");
        Assert.True(new FileInfo(path).Length > 0, $"Файл пуст: {path}");
    }

    /// <summary>
    /// nginx-конфиг копируется в образ, и путь в Dockerfile обязан совпадать с
    /// реальным файлом: иначе сборка падает уже на COPY.
    /// </summary>
    [Fact]
    public void Nginx_config_exists_where_dockerfile_expects_it()
    {
        var dockerfile = File.ReadAllText(
            FindRepositoryFile("src", "MARS.Gateway", "ClientApp.Dockerfile")
        );

        var match = NginxConfigCopy().Match(dockerfile);

        Assert.True(
            match.Success,
            "Dockerfile клиента не копирует infrastructure/nginx/client-ui.conf"
        );

        var path = match.Groups["path"].Value;
        var exists = false;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !exists)
        {
            var candidate = Path.Combine(
                directory.FullName,
                path.Replace('/', Path.DirectorySeparatorChar)
            );
            exists = File.Exists(candidate);
            directory = directory.Parent;
        }

        Assert.True(exists, $"nginx-конфиг не найден: {path}");
    }

    [GeneratedRegex(
        @"- service: client-ui\s*\r?\n\s*project: (?<project>[A-Za-z0-9._-]+)\s*\r?\n\s*dockerfile: (?<dockerfile>[A-Za-z0-9._-]+)"
    )]
    private static partial Regex ClientUiRow();

    [GeneratedRegex(@"(?<path>infrastructure/nginx/[A-Za-z0-9._-]+\.conf)")]
    private static partial Regex NginxConfigCopy();
}
