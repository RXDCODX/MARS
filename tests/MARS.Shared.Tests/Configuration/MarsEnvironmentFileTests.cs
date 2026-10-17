using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Configuration;

/// <summary>
/// Чтение файла окружения как источника конфигурации.
/// </summary>
/// <remarks>
/// Файл окружения читает compose, и на этом всё: сервис, запущенный на хосте, его
/// не видел вообще, а профиль запуска секреты в себе не держит. Разрыв и был
/// причиной того, что токены Twitch и настройки Telegram приходилось задавать
/// заново и вручную. Файл дозаполняет конфигурацию — только отсутствующие ключи,
/// поэтому переменная окружения и профиль запуска главнее по построению.
/// </remarks>
public class MarsEnvironmentFileTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mars-envfile-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Разбор как у compose: комментарии, пустые строки, кавычки и префикс
    /// <c>export</c> не должны попасть в значение.
    /// </summary>
    [Fact]
    public void Разбирает_значения_как_compose()
    {
        Write(
            "# комментарий",
            "",
            "Twitch__Secret=простой",
            "TWITCH_OAUTH=\"в кавычках\"",
            "export SPACED='в одинарных'",
            "  ОТСТУП =  с пробелами  "
        );

        var values = Read();

        Assert.Equal("простой", values["Twitch__Secret"]);
        Assert.Equal("в кавычках", values["TWITCH_OAUTH"]);
        Assert.Equal("в одинарных", values["SPACED"]);
        Assert.Equal("с пробелами", values["ОТСТУП"]);
    }

    /// <summary>
    /// Значение с <c>=</c> внутри не должно распадаться на части: такие строки
    /// есть у строк подключения.
    /// </summary>
    [Fact]
    public void Сохраняет_значение_с_равенством()
    {
        Write("ConnectionStrings__TwitchDb=Host=localhost;Port=6017");

        Assert.Equal("Host=localhost;Port=6017", Read()["ConnectionStrings__TwitchDb"]);
    }

    /// <summary>
    /// Файла нет — это не ошибка: контейнеру он не нужен, там значения приходят
    /// переменными окружения.
    /// </summary>
    [Fact]
    public void Отсутствующий_файл_не_мешает()
    {
        var manager = new ConfigurationManager();

        var added = MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Null(added);
        Assert.Null(manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Вне Development файл не читается: в контейнерах боевого стенда секреты
    /// приходят переменными окружения, а файл туда не попадает по
    /// <c>.dockerignore</c>. Ещё один источник в бою — значит усложнить путь, по
    /// которому приходит боевое окружение.
    /// </summary>
    [Fact]
    public void Вне_Development_файл_не_читается()
    {
        Write("Twitch__Secret=секрет");

        var manager = new ConfigurationManager();

        var added = MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: false);

        Assert.Null(added);
        Assert.Null(manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Файл дозаполняет конфигурацию — в этом весь смысл: секрет из файла стенда
    /// попадает в сервис, запущенный на хосте.
    /// </summary>
    [Fact]
    public void Файл_достраивает_конфигурацию()
    {
        Write("Twitch__Secret=из-файла", "TELEGRAM_ADMIN_ID=402763435");

        var manager = new ConfigurationManager();

        MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Equal("из-файла", manager["Twitch:Secret"]);
        Assert.Equal("402763435", manager["TELEGRAM_ADMIN_ID"]);
    }

    /// <summary>
    /// Переменная окружения и профиль запуска главнее файла: ключ, который уже
    /// задан, файлом не перебивается. Иначе правка файла стенда тихо перебила бы
    /// значения из <c>launchSettings.json</c>.
    /// </summary>
    [Fact]
    public void Переменная_окружения_главнее_файла()
    {
        Write("Twitch__Secret=из-файла");

        var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Twitch:Secret"] = "из-окружения" }
        );

        MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Equal("из-окружения", manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Пустая строка из профиля — тоже заданное значение: файлом она не
    /// перебивается, иначе «переменная есть, но пустая» вела бы себя как «нет».
    /// </summary>
    [Fact]
    public void Пустое_значение_из_окружения_файлом_не_перебивается()
    {
        Write("Twitch__Secret=из-файла");

        var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(new Dictionary<string, string?> { ["Twitch:Secret"] = "" });

        MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Equal("", manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Каталог содержимого сервиса — это <c>src/MARS.X</c>, а файл лежит в корне
    /// репозитория, то есть на два уровня выше. Поиск обязан идти вверх.
    /// </summary>
    [Fact]
    public void Файл_ищется_вверх_по_дереву()
    {
        Write("Twitch__Secret=из-корня");
        var nested = Directory.CreateDirectory(Path.Combine(_root.FullName, "src", "MARS.X"));

        var manager = new ConfigurationManager();

        var added = MarsEnvironmentFile.AddIfPresent(manager, nested.FullName, isDevelopment: true);

        Assert.Equal(Path.Combine(_root.FullName, ".env.development"), added);
        Assert.Equal("из-корня", manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Общий <c>.env</c> — запасной: он не задевает стенд, который уже поднят по
    /// <c>.env.development</c>.
    /// </summary>
    [Fact]
    public void Без_Development_берётся_общий_env()
    {
        File.WriteAllLines(Path.Combine(_root.FullName, ".env"), ["Twitch__Secret=из-.env"]);

        var manager = new ConfigurationManager();

        MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Equal("из-.env", manager["Twitch:Secret"]);
    }

    /// <summary>
    /// Перевод разделителя секций: в переменной окружения секции разделяет двойное
    /// подчёркивание, в конфигурации — двоеточие.
    /// </summary>
    [Theory]
    [InlineData("Twitch__OAuth", "Twitch:OAuth")]
    [InlineData("Telegram__AdminIds__0", "Telegram:AdminIds:0")]
    [InlineData("TWITCH_OAUTH", "TWITCH_OAUTH")]
    public void Разделитель_секций_переводится_в_двоеточие(string переменная, string ключ)
    {
        Assert.Equal(ключ, MarsEnvironmentFile.ToConfigurationKey(переменная));
    }

    /// <summary>
    /// Вся цепочка целиком, потому что каждый её шаг уже стоил отдельной правки:
    /// имя из файла окружения — имя переменной compose, сопоставление с ключом
    /// секции объявлено в compose, а разделителем секций остаётся двоеточие.
    /// </summary>
    [Fact]
    public void Значение_Попадает_В_Ключ_Секции()
    {
        Write("TWITCH_OAUTH=токен-чата", "TELEGRAM_ADMIN_ID=402763435");
        File.WriteAllLines(
            Path.Combine(_root.FullName, "docker-compose.yml"),
            [
                "      Twitch__OAuth: ${TWITCH_OAUTH:-}",
                "      Telegram__AdminIds__0: \"${TELEGRAM_ADMIN_ID:-}\"",
            ]
        );

        var manager = new ConfigurationManager();

        MarsEnvironmentFile.AddIfPresent(manager, _root.FullName, isDevelopment: true);

        Assert.Equal("токен-чата", manager["Twitch:OAuth"]);
        Assert.Equal("402763435", manager["Telegram:AdminIds:0"]);
        Assert.Null(manager["TWITCH_OAUTH"]);
    }

    private Dictionary<string, string?> Read() =>
        MarsEnvironmentFile.Read(Path.Combine(_root.FullName, ".env.development"));

    private void Write(params string[] lines) =>
        File.WriteAllLines(Path.Combine(_root.FullName, ".env.development"), lines);
}
