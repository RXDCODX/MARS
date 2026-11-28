using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Gateway.Tests;

/// <summary>
/// depends_on описывает только жёсткие требования запуска, а не весь граф YARP.
/// </summary>
/// <remarks>
/// Общий depends_on в x-service-defaults делал холодный старт каскадным: сервис
/// без собственной БД или Wolverine ждал лишнюю инфраструктуру, а Gateway ждал
/// несколько backend-сервисов. Проверка фиксирует реальные требования каждого
/// сервиса, чтобы лишняя зависимость не вернулась вместе с новым compose-блоком.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ComposeDependencyTests
{
    [Fact]
    public void ОбщийAnchorНеЗадаётЗависимости()
    {
        var text = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml")
        );
        var start = text.IndexOf("x-service-defaults:", StringComparison.Ordinal);
        var end = text.IndexOf("\nservices:", start, StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start, "Не найден x-service-defaults.");
        Assert.DoesNotContain("depends_on:", text[start..end], StringComparison.Ordinal);
    }

    [Fact]
    public void ЗависимостиСервисовСовпадаютСИхЖёсткимиТребованиями()
    {
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["twitch-core"] = ["postgres", "rabbitmq"],
            ["waifu-gacha"] = ["postgres", "rabbitmq"],
            ["telegram"] = ["postgres", "rabbitmq"],
            ["discord"] = ["rabbitmq"],
            ["commands"] = ["postgres", "rabbitmq"],
            ["sound-request"] = ["postgres"],
            ["tts"] = [],
            ["obs"] = ["media-storage"],
            ["videos365"] = ["postgres"],
            ["shikimori"] = ["postgres"],
            ["alerts"] = ["media-storage", "postgres", "rabbitmq"],
            ["scoreboard"] = ["postgres"],
            ["cinema-queue"] = ["postgres"],
            ["media-storage"] = ["postgres"],
            ["admin"] = ["postgres", "rabbitmq"],
            ["gateway"] = [],
            ["client-ui"] = [],
        };

        foreach (var (service, dependencies) in expected)
        {
            var actual = Dependencies(ComposeFile.Block(service));
            var expectedSet = dependencies.ToHashSet(StringComparer.Ordinal);

            Assert.True(
                actual.SetEquals(expectedSet),
                $"{service}: ожидались [{string.Join(", ", expectedSet.Order())}], "
                    + $"а указаны [{string.Join(", ", actual.Order())}]."
            );
        }
    }

    [Fact]
    public void DevHealthcheckДаётВремяНаХолоднуюСборку()
    {
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.dev.yml");
        var text = File.ReadAllText(path);
        var start = text.IndexOf("x-dev-defaults:", StringComparison.Ordinal);
        var end = text.IndexOf("\nservices:", start, StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start, "Не найден x-dev-defaults.");
        Assert.Contains("start_period: 600s", text[start..end], StringComparison.Ordinal);
        Assert.Contains(
            "start_period: 600s",
            ComposeFile.Block("client-ui", "docker-compose.dev.yml"),
            StringComparison.Ordinal
        );
    }

    private static HashSet<string> Dependencies(string block)
    {
        var lines = block.Split('\n');
        var start = Array.FindIndex(lines, line => line.Trim() == "depends_on:");

        if (start < 0)
        {
            return [];
        }

        var dependencies = new HashSet<string>(StringComparer.Ordinal);

        for (var index = start + 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var match = Regex.Match(line, @"^ {6}([A-Za-z0-9_.-]+):");

            if (match.Success)
            {
                dependencies.Add(match.Groups[1].Value);
                continue;
            }

            if (line.StartsWith("        ", StringComparison.Ordinal))
            {
                continue;
            }

            break;
        }

        return dependencies;
    }
}
