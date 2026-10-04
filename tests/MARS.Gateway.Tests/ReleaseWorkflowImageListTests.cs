using System.Text.RegularExpressions;
using Xunit;

namespace MARS.Gateway.Tests;

/// <summary>
/// Сводка релиза против матрицы публикации.
/// </summary>
/// <remarks>
/// Задача <c>summary</c> в <c>release-microservices.yml</c> печатает в
/// <c>GITHUB_STEP_SUMMARY</c> список образов. Список задан строкой рядом с
/// матрицей <c>publish</c>, то есть это вторая копия одних и тех же данных, и
/// однажды они разошлись: <c>client-ui</c> и <c>shikimori</c> публиковались, но в
/// итог релиза не попадали. Сводка при этом утверждала список и выглядела
/// правдиво.
/// </remarks>
/// <remarks>
/// Единственная защита — сверка в тесте. Взять список из матрицы нельзя: у
/// матричной задачи выходы перезаписываются той ногой, которая завершилась
/// последней, а матрица нужна ещё и для <c>project</c>/<c>dockerfile</c>.
/// </remarks>
public partial class ReleaseWorkflowImageListTests
{
    private static string WorkflowText()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "release-microservices.yml");

        Assert.True(File.Exists(path), $"Не найден release-microservices.yml: {path}");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Имена сервисов из матрицы <c>publish</c>.
    /// </summary>
    [GeneratedRegex(@"^\s{10}- service:\s*(\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex MatrixServiceRegex();

    /// <summary>Имена сервисов из строки сводки.</summary>
    [GeneratedRegex(@"services=""([^""]+)""")]
    private static partial Regex SummaryServicesRegex();

    [Fact]
    public void Summary_lists_exactly_the_published_images()
    {
        var text = WorkflowText();

        var matrix = MatrixServiceRegex()
            .Matches(text)
            .Select(match => match.Groups[1].Value)
            .ToList();

        Assert.NotEmpty(matrix);

        var summaryMatch = SummaryServicesRegex().Match(text);

        Assert.True(
            summaryMatch.Success,
            "В release-microservices.yml не найдена строка `services=\"…\"` задачи summary."
        );

        var summary = summaryMatch
            .Groups[1]
            .Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        Assert.Equal(
            matrix.OrderBy(name => name, StringComparer.Ordinal),
            summary.OrderBy(name => name, StringComparer.Ordinal)
        );
    }

    [Fact]
    public void Matrix_and_summary_contain_no_duplicates()
    {
        var text = WorkflowText();

        var matrix = MatrixServiceRegex()
            .Matches(text)
            .Select(match => match.Groups[1].Value)
            .ToList();

        Assert.Equal(matrix.Count, matrix.Distinct(StringComparer.Ordinal).Count());

        var summary = SummaryServicesRegex()
            .Match(text)
            .Groups[1]
            .Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        Assert.Equal(summary.Count, summary.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Каждый образ матрицы пригоден для публикации.
    /// </summary>
    /// <remarks>
    /// Без <c>dockerfile</c> шаг сборки взял бы <c>MARS.Gateway/Dockerfile</c> и
    /// опубликовал .NET-образ Gateway под именем <c>mars-client-ui</c>: задача
    /// осталась бы зелёной, а стенд — сломанным. Поэтому у клиента это поле
    /// обязательно.
    /// </remarks>
    [Fact]
    public void Image_without_a_dotnet_project_declares_its_own_dockerfile()
    {
        var text = WorkflowText();

        var entries = Regex.Matches(
            text,
            @"- service:\s*(?<service>\S+)\r?\n\s+project:\s*(?<project>\S+)(?<extra>\r?\n\s+dockerfile:\s*\S+)?",
            RegexOptions.None
        );

        Assert.NotEmpty(entries);

        foreach (Match entry in entries)
        {
            var service = entry.Groups["service"].Value;

            if (service == "client-ui")
            {
                Assert.True(
                    entry.Groups["extra"].Success,
                    "Образ client-ui собирается из Vite и nginx, поэтому у него обязан быть свой Dockerfile."
                );
            }
        }
    }
}
