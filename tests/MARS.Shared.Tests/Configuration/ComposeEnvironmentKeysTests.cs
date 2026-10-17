using MARS.Shared.Configuration;

namespace MARS.Shared.Tests.Configuration;

/// <summary>
/// Разбор сопоставления «переменная файла окружения → ключ конфигурации».
/// </summary>
/// <remarks>
/// Сопоставление берётся из <c>docker-compose.yml</c>, а не из списка в коде: там
/// оно записано единственный раз, и копия разъехалась бы молча — ровно тот класс
/// опечаток, из-за которого уже расходились строки подключения и имя секции.
/// </remarks>
public class ComposeEnvironmentKeysTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mars-compose-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Обычная строка раздела <c>environment</c>, со значением по умолчанию.
    /// </summary>
    [Fact]
    public void Читает_подстановку_со_значением_по_умолчанию()
    {
        var keys = Read("Twitch__OAuth: ${TWITCH_OAUTH:-}");

        Assert.Equal("Twitch__OAuth", keys["TWITCH_OAUTH"]);
    }

    /// <summary>
    /// Подстановка без значения по умолчанию и в кавычках — тоже обычное дело.
    /// </summary>
    [Fact]
    public void Читает_подстановку_без_умолчания_и_в_кавычках()
    {
        var keys = Read(
            "Telegram__BotToken: \"${TELEGRAM_BOT_TOKEN:-}\"",
            "ASPNETCORE_ENVIRONMENT: Development"
        );

        Assert.Equal("Telegram__BotToken", keys["TELEGRAM_BOT_TOKEN"]);
        Assert.False(keys.ContainsKey("ASPNETCORE_ENVIRONMENT"));
    }

    /// <summary>
    /// Строка подключения с подстановкой внутри — не сопоставление: переменная
    /// <c>MARS_TWITCH_PASSWORD</c> не становится ключом конфигурации.
    /// </summary>
    [Fact]
    public void Строка_подключения_не_сопоставляется()
    {
        var keys = Read(
            "ConnectionStrings__TwitchDb: \"Host=postgres;Password=${MARS_TWITCH_PASSWORD:-mars}\""
        );

        Assert.Empty(keys);
    }

    /// <summary>
    /// Список портов начинается с дефиса и сопоставлением не является: иначе в
    /// конфигурацию каждого сервиса попали бы <c>MARS_PORT_*</c>.
    /// </summary>
    [Fact]
    public void Список_портов_не_сопоставляется()
    {
        var keys = Read("      - \"${MARS_PORT_GATEWAY}:8080\"");

        Assert.Empty(keys);
    }

    /// <summary>
    /// Одна и та же переменная в оверрайде указывает на другой ключ — и
    /// выигрывает оверрайд: файлы читаются в том же порядке, в каком compose их
    /// сливает.
    /// </summary>
    [Fact]
    public void Оверрейд_заменяет_сопоставление_переменной()
    {
        File.WriteAllLines(
            Path.Combine(_root.FullName, "docker-compose.yml"),
            ["Twitch__OAuth: ${TWITCH_OAUTH:-}"]
        );
        File.WriteAllLines(
            Path.Combine(_root.FullName, "docker-compose.dev.yml"),
            ["Twitch__ChatToken: ${TWITCH_OAUTH:-}"]
        );

        var keys = ComposeEnvironmentKeys.Read(_root.FullName);

        Assert.Equal("Twitch__ChatToken", keys["TWITCH_OAUTH"]);
    }

    /// <summary>
    /// Файлов compose нет — не ошибка: имена попадут в конфигурацию как есть,
    /// ровно так же, как они дошли бы до контейнера переменными окружения.
    /// </summary>
    [Fact]
    public void Без_Файлов_compose_сопоставление_пустое()
    {
        Assert.Empty(ComposeEnvironmentKeys.Read(_root.FullName));
    }

    private Dictionary<string, string> Read(params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_root.FullName, "docker-compose.yml"), lines);

        return ComposeEnvironmentKeys.Read(_root.FullName);
    }
}
