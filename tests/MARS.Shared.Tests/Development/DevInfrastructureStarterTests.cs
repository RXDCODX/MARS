using MARS.Shared.Development;
using MARS.TestKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Решения стартера автостарта: поднимать ли compose, что именно и что сказать в
/// лог. Ни один настоящий docker здесь не запускается — проба портов и раннер
/// процесса подставные, потому что предмет проверки — решение, а не сам docker.
/// </summary>
/// <remarks>
/// Временный корень репозитория на каждый тест: стартер ищет docker-compose.yml
/// вверх от каталога содержимого, и общий каталог сделал бы тесты зависимыми
/// друг от друга.
/// </remarks>
public class DevInfrastructureStarterTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mars-dev-infra-");
    private readonly FakeProbe _probe = new();
    private readonly FakeRunner _runner = new();
    private readonly CollectingLogger _logger = new();
    private readonly TestHostEnvironment _environment = new();
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Dev:AutoStartInfra"] = "true",
        ["ConnectionStrings:MarsDb"] = "Host=localhost;Port=6017;Database=mars",
        ["RabbitMq:Host"] = "localhost",
        ["RabbitMq:Port"] = "6018",
        ["Otlp:Endpoint"] = "http://localhost:6021",
    };

    public DevInfrastructureStarterTests()
    {
        _environment.EnvironmentName = Environments.Development;
        _environment.ContentRootPath = _root.FullName;

        WriteComposeFile();
        WriteEnvironmentFile(
            template: ["MARS_PORT_POSTGRES=6017"],
            file: ["MARS_PORT_POSTGRES=6017"]
        );
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Адреса, на которые проба отвечает «слушаю».</summary>
    private static readonly string[] Postgres = ["localhost:6017"];

    private static readonly string[] RabbitMq = ["localhost:6018"];

    private static readonly string[] Tempo = ["localhost:6021"];

    private DevInfrastructureStarter Starter() =>
        new(
            _environment,
            new ConfigurationBuilder().AddInMemoryCollection(_settings).Build(),
            _probe,
            _runner,
            _logger
        );

    private void WriteComposeFile() =>
        File.WriteAllText(
            Path.Combine(_root.FullName, DevInfrastructureCompose.ComposeFile),
            "services: {}"
        );

    private void WriteEnvironmentFile(string[] template, string[] file)
    {
        File.WriteAllLines(
            Path.Combine(_root.FullName, DevInfrastructureCompose.DevEnvironmentTemplateFile),
            template
        );
        File.WriteAllLines(
            Path.Combine(_root.FullName, DevInfrastructureCompose.DevEnvironmentFile),
            file
        );
    }

    /// <summary>
    /// Флаг выключен — docker не трогаем вовсе. Проверяется и проба: в контейнере
    /// она ходила бы на имена служб compose.
    /// </summary>
    [Fact]
    public void При_выключенном_флаге_docker_и_проба_не_дёргаются()
    {
        _settings["Dev:AutoStartInfra"] = "false";

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Empty(_probe.Calls);
    }

    /// <summary>
    /// Запуск без профиля <c>MARS.Local</c>: секции <c>Dev</c> в конфигурации нет
    /// вовсе. Так выглядит F5 в Visual Studio — IDE подхватывает профиль с именем
    /// проекта, где ключей <c>Dev</c> нет. Раньше это заканчивалось тихим отказом и
    /// выглядело как «автостарт сломан»: ни контейнеров, ни сообщения.
    /// </summary>
    [Fact]
    public void Без_секции_Dev_в_Development_автостарт_срабатывает()
    {
        _settings.Remove("Dev:AutoStartInfra");

        Starter().EnsureStarted();

        Assert.NotEmpty(_runner.Calls);
    }

    [Fact]
    public void Вне_Development_docker_и_проба_не_дёргаются()
    {
        _environment.EnvironmentName = Environments.Production;

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Empty(_probe.Calls);
    }

    /// <summary>
    /// Выключенный флаг в Development — это почти всегда запуск не тем профилем,
    /// поэтому молчать нельзя. Именно тихий отпуск и выглядел как «автостарт
    /// сломан»: контейнеры не создавались, а в консоли не было ни слова.
    /// </summary>
    [Fact]
    public void Выключенный_флаг_в_Development_предупреждает()
    {
        _settings["Dev:AutoStartInfra"] = "false";

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Contains(
            _logger.Warnings,
            warning => warning.Contains("Dev:AutoStartInfra", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Вне Development то же самое — обычное дело, а не ошибка разработчика:
    /// предупреждение на каждом старте контейнера было бы шумом в бою.
    /// </summary>
    [Fact]
    public void Вне_Development_выключенный_флаг_молчит()
    {
        _settings["Dev:AutoStartInfra"] = "false";
        _environment.EnvironmentName = Environments.Production;

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.DoesNotContain(
            _logger.Warnings,
            warning => warning.Contains("Dev:AutoStartInfra", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Контейнер: адреса — имена служб compose, локальных среди них нет. Такой
    /// старт не должен даже пробовать порты.
    /// </summary>
    [Fact]
    public void Без_локальных_адресов_ничего_не_поднимается()
    {
        _settings["ConnectionStrings:MarsDb"] = "Host=postgres;Port=5432;Database=mars";
        _settings["RabbitMq:Host"] = "rabbitmq";
        _settings["RabbitMq:Port"] = "5672";
        _settings["Otlp:Endpoint"] = "http://tempo:4317";

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Empty(_probe.Calls);
        Assert.Contains(
            _logger.Messages,
            message => message.Contains("нет локальных адресов", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Отвечающая_инфраструктура_не_вызывает_docker()
    {
        _probe.Listening.UnionWith([.. Postgres, .. RabbitMq, .. Tempo]);

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Contains(
            _logger.Messages,
            message => message.Contains("docker не вызывается", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Командная строка целиком: имена файлов, порядок флагов, срок ожидания и
    /// список служб. Ошибка в любом из них видна только на живом стенде.
    /// </summary>
    [Fact]
    public void Молчащая_инфраструктура_поднимается_двумя_фазами()
    {
        Starter().EnsureStarted();

        Assert.Equal(2, _runner.Calls.Count);

        var blocking = _runner.Calls[0];

        Assert.Equal(DevInfrastructureCompose.Tool, blocking.FileName);
        Assert.Equal(_root.FullName, blocking.WorkingDirectory);
        Assert.True(blocking.StreamOutput);
        Assert.Equal(TimeSpan.FromSeconds(660), blocking.Timeout);
        Assert.Equal(
            [
                "compose",
                "--env-file",
                ".env.development",
                "-f",
                "docker-compose.yml",
                "-f",
                "docker-compose.dev.yml",
                "up",
                "-d",
                "--wait",
                "--wait-timeout",
                "600",
                "postgres",
                "rabbitmq",
            ],
            blocking.Arguments
        );

        var background = _runner.Calls[1];

        Assert.False(background.StreamOutput);
        Assert.Equal(TimeSpan.FromSeconds(600), background.Timeout);
        Assert.Equal(
            [
                "compose",
                "--env-file",
                ".env.development",
                "-f",
                "docker-compose.yml",
                "-f",
                "docker-compose.dev.yml",
                "up",
                "-d",
                "tempo",
                "prometheus",
                "grafana",
                "loki",
                "alloy",
            ],
            background.Arguments
        );
    }

    /// <summary>
    /// База и брокер уже отвечают, а tempo молчит: блокирующая фаза обязана
    /// пропустить свои две службы, иначе <c>up -d</c> без имён поднимет весь стенд
    /// вместе с четырнадцатью сервисами.
    /// </summary>
    [Fact]
    public void Отвечающие_службы_в_блокирующую_фазу_не_попадают()
    {
        _probe.Listening.UnionWith([.. Postgres, .. RabbitMq]);

        Starter().EnsureStarted();

        var call = Assert.Single(_runner.Calls);

        Assert.DoesNotContain("postgres", call.Arguments);
        Assert.Contains("tempo", call.Arguments);
    }

    /// <summary>
    /// Своей строки подключения у сервиса нет (иначе говоря, базы у него нет), и
    /// источника пробы тоже — такую службу поднимаем: лишний идемпотентный
    /// <c>up</c> дешевле, чем сервис без базы.
    /// </summary>
    [Fact]
    public void Служба_без_источника_пробы_поднимается()
    {
        _settings.Remove("ConnectionStrings:MarsDb");

        Starter().EnsureStarted();

        Assert.Equal(2, _runner.Calls.Count);
        Assert.Contains("postgres", _runner.Calls[0].Arguments);
    }

    [Fact]
    public void Неизвестное_состояние_пробы_считается_поводом_поднять()
    {
        _probe.Fallback = DevEndpointState.Unknown;

        Starter().EnsureStarted();

        Assert.Equal(2, _runner.Calls.Count);
    }

    [Fact]
    public void Без_корня_репозитория_предупреждение_и_ничего_больше()
    {
        var isolated = Directory.CreateTempSubdirectory("mars-no-compose-");

        try
        {
            _environment.ContentRootPath = isolated.FullName;

            Starter().EnsureStarted();

            Assert.Empty(_runner.Calls);
            Assert.Contains(
                _logger.Warnings,
                warning =>
                    warning.Contains("корень репозитория не найден", StringComparison.Ordinal)
            );
        }
        finally
        {
            isolated.Delete(recursive: true);
        }
    }

    [Fact]
    public void Без_файла_окружения_подсказка_как_поднять_стенд()
    {
        File.Delete(Path.Combine(_root.FullName, DevInfrastructureCompose.DevEnvironmentFile));

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Contains(
            _logger.Warnings,
            warning =>
                warning.Contains(
                    DevInfrastructureCompose.DevEnvironmentFile,
                    StringComparison.Ordinal
                )
        );
    }

    /// <summary>
    /// Отставший файл окружения — предупреждение, а не запрет: ключа может не быть
    /// у службы, которая этому сервису не нужна.
    /// </summary>
    [Fact]
    public void Отставший_файл_окружения_предупреждает_но_не_запрещает_запуск()
    {
        WriteEnvironmentFile(
            template: ["MARS_PORT_POSTGRES=6017", "MARS_PORT_GATEWAY=7001", "MARS_PORT_MATOI=6027"],
            file: ["MARS_PORT_POSTGRES=6017"]
        );

        Starter().EnsureStarted();

        Assert.Equal(2, _runner.Calls.Count);
        Assert.Contains(
            _logger.Warnings,
            warning =>
                warning.Contains("MARS_PORT_GATEWAY", StringComparison.Ordinal)
                && warning.Contains("MARS_PORT_MATOI", StringComparison.Ordinal)
        );
    }

    /// <summary>Отказ compose не бросает: он идёт предупреждением с хвостом вывода.</summary>
    [Fact]
    public void Отказ_compose_не_бросает_и_показывает_хвост()
    {
        _runner.Answers.Enqueue(
            new DevProcessResult(1, "create postgres", "port is already allocated")
        );

        Starter().EnsureStarted();

        Assert.Single(_runner.Calls);
        Assert.Contains(
            _logger.Warnings,
            warning =>
                warning.Contains("База и брокер не поднялись", StringComparison.Ordinal)
                && warning.Contains("port is already allocated", StringComparison.Ordinal)
                && warning.Contains("create postgres", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Таймаут_compose_тоже_предупреждение()
    {
        _runner.Answers.Enqueue(
            new DevProcessResult(-1, string.Empty, string.Empty, TimedOut: true)
        );

        Starter().EnsureStarted();

        Assert.Single(_runner.Calls);
        Assert.Contains(
            _logger.Warnings,
            warning => warning.Contains("истёк предельный срок", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Погашенный_daemon_подсказывает_про_Docker_Desktop()
    {
        _runner.Answers.Enqueue(
            new DevProcessResult(
                1,
                string.Empty,
                "Cannot connect to the Docker daemon at npipe:////./pipe/docker_engine. Is the docker daemon running?"
            )
        );

        Starter().EnsureStarted();

        Assert.Contains(
            _logger.Warnings,
            warning => warning.Contains("Docker Desktop не запущен", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Отсутствующий_docker_подсказывает_про_установку()
    {
        _runner.Answers.Enqueue(
            new DevProcessResult(
                -1,
                string.Empty,
                string.Empty,
                false,
                "Не удалось выполнить docker: система не нашла файл"
            )
        );

        Starter().EnsureStarted();

        Assert.Contains(
            _logger.Warnings,
            warning => warning.Contains("docker не найден в PATH", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Отказ наблюдаемости запуск сервиса не срывает: трейсы и панели можно не
    /// увидеть, работать сервис будет.
    /// </summary>
    [Fact]
    public void Отказ_наблюдаемости_идёт_в_лог_и_не_срывает_запуск()
    {
        _runner.Answers.Enqueue(new DevProcessResult(0, string.Empty, string.Empty));
        _runner.Answers.Enqueue(new DevProcessResult(1, string.Empty, "образ не скачался"));

        Starter().EnsureStarted();

        Assert.Equal(2, _runner.Calls.Count);
        Assert.Contains(
            _logger.Warnings,
            warning => warning.Contains("Наблюдаемость не поднялась", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Проба бросает исключение — старт сервиса продолжается: подсказка при отладке
    /// не стоит того, чтобы стать причиной несостоявшегося запуска.
    /// </summary>
    [Fact]
    public void Падение_пробы_не_роняет_старт()
    {
        _probe.Throw = true;

        Starter().EnsureStarted();

        Assert.Empty(_runner.Calls);
        Assert.Contains(
            _logger.Warnings,
            warning =>
                warning.Contains("Автостарт инфраструктуры не выполнен", StringComparison.Ordinal)
        );
    }

    /// <summary>Подставная проба: отвечает по списку адресов, вызовы записывает.</summary>
    private sealed class FakeProbe : IDevEndpointProbe
    {
        public List<string> Calls { get; } = [];

        public HashSet<string> Listening { get; } = [];

        public DevEndpointState Fallback { get; set; } = DevEndpointState.NotListening;

        public bool Throw { get; set; }

        public DevEndpointState Probe(string host, int port)
        {
            if (Throw)
            {
                throw new InvalidOperationException("проба недоступна");
            }

            var address = $"{host}:{port}";
            Calls.Add(address);

            return Listening.Contains(address) ? DevEndpointState.Listening : Fallback;
        }
    }

    /// <summary>Подставной раннер: записывает запросы, отвечает по очереди.</summary>
    private sealed class FakeRunner : IDevProcessRunner
    {
        public List<DevProcessRequest> Calls { get; } = [];

        public Queue<DevProcessResult> Answers { get; } = new();

        public DevProcessResult Run(DevProcessRequest request)
        {
            Calls.Add(request);

            return Answers.Count > 0
                ? Answers.Dequeue()
                : new DevProcessResult(0, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Свой логгер, а не <c>Mock&lt;ILogger&gt;</c>: тип-матчер в колбэке Moq не
    /// работает, а подписка на сообщения нужна всем проверкам решений.
    /// </summary>
    private sealed class CollectingLogger : ILogger<DevInfrastructureStarter>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IEnumerable<string> Messages => Entries.Select(entry => entry.Message);

        public IEnumerable<string> Warnings =>
            Entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Entries.Add((logLevel, formatter(state, exception)));
    }
}
