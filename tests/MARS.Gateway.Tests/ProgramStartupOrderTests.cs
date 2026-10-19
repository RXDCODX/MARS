namespace MARS.Gateway.Tests;

/// <summary>
/// Порядок шагов в <c>Program.cs</c>, от которого зависит автостарт инфраструктуры.
/// </summary>
/// <remarks>
/// <para>
/// Поднятие инфраструктуры живёт в <c>UseMarsDefaults</c>, а миграции применяются
/// вызовом <c>RunMarsSchemaMigrationsAsync</c> прямо в <c>Program.cs</c>. Порядок
/// этих двух вызовов — не стилистика: поменяй местами, и миграции пойдут в базу,
/// которую ещё никто не поднял, то есть F5 снова ждёт базу минуту и падает.
/// Компилятор и остальные тесты этого не заметят: обе строки на месте, просто не в
/// том порядке.
/// </para>
/// <para>
/// Файлы читаются как текст: вызов в <c>Program.cs</c> верхнего уровня статически
/// не проверить, а шестнадцать сервисов дают шестнадцать мест, где это может
/// разойтись — правка одного <c>Program.cs</c> не видна в diff остальных.
/// </para>
/// </remarks>
public class ProgramStartupOrderTests
{
    /// <summary>Шаг, который поднимает инфраструктуру стенда.</summary>
    private const string Infrastructure = "UseMarsDefaults(";

    /// <summary>Шаг, который ждёт базу: он обязан идти после.</summary>
    private const string Migrations = "RunMarsSchemaMigrationsAsync";

    /// <summary>Точки входа сервисов: <c>src/MARS.X/Program.cs</c>.</summary>
    private static List<string> Programs =>
        Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src"),
                "Program.cs",
                SearchOption.AllDirectories
            )
            .Where(path =>
                !path.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            .Where(path =>
                !path.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>Корень репозитория: каталог, где лежит <c>MARS.slnx</c>.</summary>
    private static string RepositoryRoot =>
        Path.GetDirectoryName(ClientUiImageWorkflowTests.FindRepositoryFile("MARS.slnx"))
        ?? throw new InvalidOperationException("Не удалось определить корень репозитория.");

    /// <summary>
    /// Поднятие инфраструктуры стоит до применения миграций — во всех сервисах.
    /// </summary>
    [Fact]
    public void Инфраструктура_поднимается_до_миграций_в_каждом_сервисе()
    {
        var programs = Programs;

        Assert.NotEmpty(programs);

        foreach (var program in programs)
        {
            var text = File.ReadAllText(program);
            var name = Path.GetFileName(Path.GetDirectoryName(program));
            var infrastructure = text.IndexOf(Infrastructure, StringComparison.Ordinal);
            var migrations = text.IndexOf(Migrations, StringComparison.Ordinal);

            Assert.True(
                infrastructure >= 0,
                $"{name}/Program.cs не зовёт {Infrastructure.TrimEnd('(')}: автостарт инфраструктуры не работает."
            );

            Assert.True(
                migrations < 0 || infrastructure < migrations,
                $"{name}/Program.cs применяет миграции до {Infrastructure.TrimEnd('(')}."
                    + " Миграции пойдут в неподнятую базу, и F5 снова будет ждать её минуту."
            );
        }
    }
}
