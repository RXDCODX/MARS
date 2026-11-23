namespace MARS.Gateway.Tests;

/// <summary>
/// Готовность каждой службы стенда: compose считает службу здоровой только при
/// наличии healthcheck с ключом <c>test</c>. Без него <c>--wait</c> возвращает
/// управление, когда контейнер лишь запустился, а не когда он готов принимать
/// запросы — и стенд «поднимается» зелёным при мёртвом сервисе.
/// </summary>
/// <remarks>
/// Службы, получающие healthcheck из якоря <c>x-service-defaults</c>
/// (<c>&lt;&lt;: *service-defaults</c>), считаются покрытыми: отдельная проверка
/// <see cref="ЯкорьServiceDefaultsОпределяетHealthcheck"/> следит, что якорь
/// действительно объявляет <c>test</c>, иначе все .NET-службы потеряли бы чек
/// одним изменением якоря.
/// </remarks>
public class ComposeHealthcheckTests
{
    private static List<string> Services()
    {
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml");
        var lines = File.ReadAllLines(path);
        var services = new List<string>();
        var inServices = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');

            if (line == "services:")
            {
                inServices = true;
                continue;
            }

            // Следующий верхнеуровневый раздел закрывает секцию services.
            if (inServices && line.Length > 0 && !line.StartsWith(' ') && !line.StartsWith('#'))
            {
                break;
            }

            if (inServices)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    line,
                    @"^  ([A-Za-z0-9_.-]+):\s*$"
                );
                if (match.Success)
                {
                    services.Add(match.Groups[1].Value);
                }
            }
        }

        Assert.NotEmpty(services);
        return services;
    }

    [Fact]
    public void КаждаяСлужбаИмеетHealthcheck()
    {
        foreach (var service in Services())
        {
            var block = ComposeFile.Block(service);

            // Либо явный healthcheck, либо наследование из якоря x-service-defaults.
            var hasAnchor = block.Contains("<<: *service-defaults", StringComparison.Ordinal);
            var hasExplicit = false;

            if (!hasAnchor)
            {
                // ComposeFile.HealthcheckTest бросает Assert, если ключа test нет.
                try
                {
                    _ = ComposeFile.HealthcheckTest(block);
                    hasExplicit = true;
                }
                catch
                {
                    hasExplicit = false;
                }
            }

            Assert.True(
                hasAnchor || hasExplicit,
                $"Служба '{service}' в docker-compose.yml не имеет healthcheck с ключом test "
                    + "и не наследует его из x-service-defaults. --wait не дождётся её готовности."
            );
        }
    }

    [Fact]
    public void ЯкорьServiceDefaultsОпределяетHealthcheck()
    {
        // Читаем файл и находим блок x-service-defaults.
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml");
        var text = File.ReadAllText(path);
        var anchorStart = text.IndexOf("x-service-defaults:", StringComparison.Ordinal);
        Assert.True(anchorStart >= 0, "В docker-compose.yml нет якоря x-service-defaults.");

        // Якорь заканчивается на следующем верхнеуровневом ключе (services:).
        var servicesStart = text.IndexOf("\nservices:", anchorStart, StringComparison.Ordinal);
        Assert.True(servicesStart >= 0, "Не найдена секция services после якоря.");

        var anchorBlock = text[anchorStart..servicesStart];
        _ = ComposeFile.HealthcheckTest(anchorBlock);
    }
}
