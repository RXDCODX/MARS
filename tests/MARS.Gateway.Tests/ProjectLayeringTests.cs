namespace MARS.Gateway.Tests;

/// <summary>
/// Имя проекта, папка и namespace — одно и то же, а слой стоит последним.
/// </summary>
/// <remarks>
/// <para>
/// Соглашение описано в <c>AGENTS.md</c> («Проекты и папки: сборка = namespace =
/// папка»). Нарушение не ломает сборку: проект с именем <c>MARS.TwitchCore</c>
/// собирается и работает ровно так же, как <c>MARS.Modules.TwitchCore.Api</c>.
/// Ловить это приходится тестом, иначе первый же новый проект заводится
/// «как удобно» и правило расходится с кодом молча.
/// </para>
/// <para>
/// Пока сервис не переведён, его проекты лежат в <see cref="Pending"/>. Список
/// только сокращается: запись, которая уже соответствует правилу, роняет тест,
/// иначе он превращается в вечный список исключений.
/// </para>
/// </remarks>
public class ProjectLayeringTests
{
    /// <summary>Проекты, которые ещё не переведены на схему «сборка = namespace».</summary>
    /// <remarks>
    /// Переезд идёт по одному сервису. Убирать имя отсюда нужно ровно тогда,
    /// когда переименован сам проект и папка, — не раньше.
    /// </remarks>
    private static readonly HashSet<string> Pending = new(StringComparer.Ordinal)
    {
        "MARS.Admin",
        "MARS.Alerts",
        "MARS.Alerts.Overlay.Application",
        "MARS.Alerts.Application",
        "MARS.Alerts.Twitch.Messages.Application",
        "MARS.Alerts.Twitch.Rewards.Application",
        "MARS.AudioController",
        "MARS.CinemaQueue",
        "MARS.Commands",
        "MARS.Discord",
        "MARS.Gateway",
        "MARS.MediaStorage",
        "MARS.OBS",
        "MARS.Scoreboard",
        "MARS.Shikimori",
        "MARS.SoundRequest",
        "MARS.Telegram",
        "MARS.TTS",
        "MARS.TwitchCore",
        "MARS.Videos365",
        "MARS.WaifuGacha",
    };

    /// <summary>Сегменты, которые могут стоять первыми после <c>MARS</c>.</summary>
    private static readonly HashSet<string> Buckets = new(StringComparer.Ordinal)
    {
        "Modules",
        "Platform",
        "Shared",
    };

    /// <summary>Допустимые последние сегменты имени проекта.</summary>
    private static readonly HashSet<string> Layers = new(StringComparer.Ordinal)
    {
        "Domain",
        "Contracts",
        "Application",
        "Infrastructure",
        "Api",
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

    /// <summary>Проверка имени по грамматике слоёв.</summary>
    /// <remarks>
    /// <c>MARS.&lt;Bucket&gt;.&lt;Имя&gt;[.&lt;Фича&gt;][.&lt;Технология&gt;].&lt;Слой&gt;</c>,
    /// причём у <c>Infrastructure</c> технология обязательна. Пустой список
    /// слоёв закрыт: <see cref="Layerless"/>.
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

            var hasShape =
                parts.Length >= 4
                && string.Equals(parts[0], "MARS", StringComparison.Ordinal)
                && Buckets.Contains(parts[1])
                && Layers.Contains(parts[^1]);
            var hasNoRepeats = parts
                .Zip(parts.Skip(1))
                .All(pair => !string.Equals(pair.First, pair.Second, StringComparison.Ordinal));
            var hasTechnology =
                !string.Equals(parts[^1], "Infrastructure", StringComparison.Ordinal)
                || parts.Length >= 5
                || !string.Equals(parts[1], "Modules", StringComparison.Ordinal);

            result = hasShape && hasNoRepeats && hasTechnology;
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
