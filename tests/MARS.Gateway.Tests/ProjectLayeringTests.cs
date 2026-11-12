namespace MARS.Gateway.Tests;

/// <summary>
/// Имя проекта, папка и namespace — одно и то же; логическое разделение следует за владельцем.
/// </summary>
/// <remarks>
/// <para>
/// Соглашение описано в skill <c>mars-project-structure</c>:
/// <c>MARS.&lt;Project&gt;.&lt;LogicalDivision&gt;[.&lt;Submodule&gt;]</c>.
/// Логическое разделение — одно из шести разрешённых, подмодуль только один.
/// Для существующего контейнера модулей scope может быть составным:
/// <c>MARS.Modules.Alerts.Application.Adhd</c>. Нарушение не ломает сборку,
/// поэтому правило проверяется тестом, а не только документацией.
/// </para>
/// <para>
/// Legacy-проекты перечислены в <see cref="Pending"/>. Список сокращается
/// по мере их предметного рефакторинга и переименования.
/// </para>
/// </remarks>
public class ProjectLayeringTests
{
    /// <summary>Существующие проекты, которые ещё не приведены к новой грамматике.</summary>
    /// <remarks>
    /// Legacy-проекты нормализуются по мере предметного рефакторинга. Убирать имя
    /// отсюда нужно только после переименования проекта, папки и всех ссылок.
    /// </remarks>
    private static readonly HashSet<string> Pending = new(StringComparer.Ordinal)
    {
        "MARS.Admin.Infrastructure.Integrations.Shikimori",
        "MARS.Admin.Infrastructure.Persistence.Postgres",
        "MARS.Alerts.Application.Twitch.Messages",
        "MARS.Alerts.Application.Twitch.Rewards",
        "MARS.Alerts.Infrastructure.Integrations",
        "MARS.AudioController",
        "MARS.CinemaQueue",
        "MARS.Commands",
        "MARS.Discord",
        "MARS.Gateway",
        "MARS.Modules.Alerts.Infrastructure.Persistence.Postgres",
        "MARS.OBS",
        "MARS.Platform.Infrastructure.Clients.Http",
        "MARS.Platform.Infrastructure.Messaging.RabbitMq",
        "MARS.Platform.Infrastructure.Observability.Logging",
        "MARS.Platform.Infrastructure.Observability.Telemetry",
        "MARS.Platform.Infrastructure.Persistence.Postgres",
        "MARS.Platform.Infrastructure.Web.AspNetCore",
        "MARS.Scoreboard",
        "MARS.Shikimori",
        "MARS.SoundRequest",
        "MARS.TTS",
        "MARS.Videos365",
    };

    /// <summary>Семь допустимых логических разделений.</summary>
    private static readonly HashSet<string> Divisions = new(StringComparer.Ordinal)
    {
        "Domain",
        "Api",
        "Models",
        "Application",
        "Infrastructure",
        "Presentation",
        "Extensions",
    };

    /// <summary>Обобщённые технические категории не являются подмодулями.</summary>
    private static readonly HashSet<string> GenericSubmodules = new(StringComparer.Ordinal)
    {
        "Integrations",
        "Persistence",
        "Messaging",
        "Clients",
        "Observability",
        "Web",
    };

    /// <summary>Библиотеки, которым слой не нужен, — список закрытый.</summary>
    private static readonly HashSet<string> Layerless = new(StringComparer.Ordinal)
    {
        "MARS.Shared.Kernel",
        "MARS.Shared.Contracts",
        "MARS.TestKit",
    };

    /// <summary>
    /// Каждый <c>.csproj</c> лежит в папке со своим именем.
    /// </summary>
    /// <remarks>
    /// Папка — это путь в решении и в <c>using</c>: файл проекта
    /// <c>MARS.Modules.Alerts.Api.csproj</c> в папке <c>MARS.Alerts</c> собирается,
    /// но найти его по имени сборки в дереве нельзя.
    /// </remarks>
    [Fact]
    public void ИмяПроектаСовпадаетСПапкой()
    {
        var mismatches = Projects()
            .Where(project =>
                !string.Equals(
                    project.Name,
                    Path.GetFileName(Path.GetDirectoryName(project.Path)),
                    StringComparison.Ordinal
                )
            )
            .Select(project => project.Name)
            .ToList();

        Assert.True(
            mismatches.Count == 0,
            $"Проект лежит не в своей папке: {string.Join(", ", mismatches)}."
        );
    }

    /// <summary>
    /// Вне <see cref="Pending"/> имя проекта соответствует грамматике слоёв.
    /// </summary>
    /// <remarks>
    /// Тестовый проект — исключение: он остаётся <c>MARS.&lt;Сервис&gt;.Tests</c>,
    /// потому что это имя обязательного статуса в branch protection.
    /// </remarks>
    [Fact]
    public void ПереведённыеПроектыСоблюдаютГрамматику()
    {
        var violations = Projects()
            .Where(project => project.Name is not null && !Pending.Contains(project.Name))
            .Where(project => !Conforms(project.Name!, project.IsTest))
            .Select(project => project.Name!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Не соответствуют грамматике: " + string.Join(", ", violations) + "."
        );
    }

    /// <summary>
    /// <see cref="Pending"/> содержит только реально существующие и всё ещё
    /// несоответствующие проекты.
    /// </summary>
    /// <remarks>
    /// Без этой проверки список исключений живёт своей жизнью: переименованный
    /// проект остаётся в нём навсегда, а новый несоответствующий проект
    /// добавляется «на минутку».
    /// </remarks>
    [Fact]
    public void PendingНеСодержитЛишнихЗаписей()
    {
        var projects = Projects().Where(project => project.Name is not null).ToList();

        var stale = Pending
            .Where(name =>
                projects.All(project =>
                    !string.Equals(project.Name, name, StringComparison.Ordinal)
                )
            )
            .ToList();

        var conforming = Pending
            .Where(name =>
                projects.Any(project =>
                    string.Equals(project.Name, name, StringComparison.Ordinal)
                    && Conforms(project.Name!, project.IsTest)
                )
            )
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"В Pending остались несуществующие проекты: {string.Join(", ", stale)}."
        );
        Assert.True(
            conforming.Count == 0,
            $"В Pending остались уже переведённые проекты: {string.Join(", ", conforming)}."
        );
    }

    /// <summary>Ни один проект не переопределяет <c>RootNamespace</c>.</summary>
    /// <remarks>
    /// Переопределение разводит имя сборки и namespace — ровно то, что схема
    /// запрещает. Проверка по тексту <c>.csproj</c>: свойство объявляется там.
    /// </remarks>
    [Fact]
    public void RootNamespaceНеПереопределяется()
    {
        var overrides = Projects()
            .Where(project =>
                File.ReadAllText(project.Path).Contains("<RootNamespace>", StringComparison.Ordinal)
            )
            .Select(project => project.DisplayPath)
            .ToList();

        Assert.True(
            overrides.Count == 0,
            $"RootNamespace переопределён: {string.Join(", ", overrides)}."
        );
    }

    /// <summary>Проверка имени по грамматике логических разделений.</summary>
    /// <remarks>
    /// <c>MARS.&lt;Project&gt;.&lt;LogicalDivision&gt;[.&lt;Submodule&gt;]</c>.
    /// Обычно Project — один сегмент; <c>Modules.&lt;Name&gt;</c> — допустимый
    /// составной scope. Host — специальная точка композиции, не входящая в
    /// шесть логических разделений. Для legacy-проектов используется
    /// отдельный список <see cref="Pending"/>.
    /// </remarks>
    private static bool Conforms(string name, bool isTest)
    {
        var result = false;

        if (Layerless.Contains(name))
        {
            result = true;
        }
        else if (isTest)
        {
            result =
                name.StartsWith("MARS.", StringComparison.Ordinal)
                && name.EndsWith(".Tests", StringComparison.Ordinal)
                && name.Length > "MARS..Tests".Length;
        }
        else
        {
            var parts = name.Split('.');
            var hasModuleScope =
                parts.Length > 2 && string.Equals(parts[1], "Modules", StringComparison.Ordinal);
            var divisionIndex = hasModuleScope ? 3 : 2;

            var hasValidScope =
                parts.Length > divisionIndex
                && parts[0] == "MARS"
                && parts.All(part => part.Length > 0)
                && parts.Take(divisionIndex).All(part => part.Length > 0);

            var isHost =
                hasValidScope
                && parts.Length == divisionIndex + 1
                && string.Equals(parts[divisionIndex], "Host", StringComparison.Ordinal);

            var hasDivision =
                hasValidScope
                && Divisions.Contains(parts[divisionIndex])
                && (parts.Length == divisionIndex + 1 || parts.Length == divisionIndex + 2);

            var hasValidSubmodule =
                parts.Length == divisionIndex + 1
                || (
                    parts.Length == divisionIndex + 2
                    && !GenericSubmodules.Contains(parts[^1])
                    && !parts.Take(divisionIndex).Contains(parts[^1], StringComparer.Ordinal)
                );

            result = isHost || (hasDivision && hasValidSubmodule);
        }

        return result;
    }

    /// <summary>Все проекты репозитория, включая тестовые и <c>MARS.TestKit</c>.</summary>
    /// <remarks>
    /// <c>ClientApp</c> и <c>ClientUi</c> — npm-проекты, <c>.csproj</c> у них нет,
    /// поэтому в список они не попадают сами. <c>bin</c>, <c>obj</c> и
    /// <c>node_modules</c> отсекаются: в них лежат копии чужих проектов.
    /// </remarks>
    private static List<ProjectFile> Projects()
    {
        var root = RepositoryRoot();

        var paths = new[] { "src", "tests" }
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder =>
                Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories)
            )
            .Where(path => !IsBuildArtifact(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        return paths
            .Select(path => new ProjectFile(
                Path.GetFileNameWithoutExtension(path),
                path,
                path.Replace(root + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/'),
                path.Contains(
                    $"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            ))
            .ToList();
    }

    /// <summary>Каталог, в который не надо заходить при поиске проектов.</summary>
    private static bool IsBuildArtifact(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}");

    /// <summary>Корень репозитория: каталог, где лежит <c>MARS.slnx</c>.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MARS.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Не удалось определить корень репозитория.");
    }

    /// <summary>Найденный файл проекта и его положение в дереве.</summary>
    private sealed record ProjectFile(string Name, string Path, string DisplayPath, bool IsTest);
}
