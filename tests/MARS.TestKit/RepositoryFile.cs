namespace MARS.TestKit;

/// <summary>
/// Поиск файлов репозитория вверх по дереву от каталога тестов.
/// </summary>
/// <remarks>
/// Тесты запускаются из <c>bin/&lt;Configuration&gt;/&lt;TFM&gt;</c>, а читать надо
/// файл из корня: иначе проверка контракта молчала бы на отсутствующем файле.
/// Приём взят из <c>tests/MARS.Gateway.Tests/ClientUiImageWorkflowTests.cs</c>,
/// но нужен и остальным наборам, поэтому живёт здесь, в общей библиотеке.
/// </remarks>
public static class RepositoryFile
{
    /// <summary>Путь к файлу репозитория по имени.</summary>
    public static string Find(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Файл не найден вверх по дереву: {relativePath}",
            relativePath
        );
    }

    /// <summary>Есть ли такой файл в репозитории; false вместо исключения.</summary>
    /// <remarks>
    /// Отдельный метод, потому что <see cref="Find"/> на отсутствующем файле
    /// бросает: так правильно для читателя, который файл ожидал найти, и
    /// неправильно для проверки «а существует ли он вообще».
    /// </remarks>
    public static bool Exists(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, relativePath)))
            {
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    /// <summary>
    /// Все <c>*.cs</c> каталога репозитория, без <c>obj</c>, <c>bin</c> и
    /// <c>node_modules</c>: их копии содержат сгенерированный код, а не
    /// регистрации сервиса.
    /// </summary>
    public static IEnumerable<string> Sources(string relativeDirectory)
    {
        var root = DirectoryOf(relativeDirectory);

        return System
            .IO.Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGenerated(path))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    /// <summary>Каталог репозитория по относительному пути.</summary>
    /// <remarks>
    /// Ищется именно каталог, а не файл: искать «<c>путь</c>/.» нельзя —
    /// <c>File.Exists</c> на таком имени всегда ложь, и проверка падала бы с
    /// FileNotFoundException вместо разбора исходников.
    /// </remarks>
    private static string DirectoryOf(string relativeDirectory)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativeDirectory);

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Каталог не найден вверх по дереву: {relativeDirectory}"
        );
    }

    private static bool IsGenerated(string path)
    {
        var separator = Path.DirectorySeparatorChar;

        return new[] { "obj", "bin", "node_modules" }.Any(folder =>
            path.Contains($"{separator}{folder}{separator}", StringComparison.Ordinal)
        );
    }
}
