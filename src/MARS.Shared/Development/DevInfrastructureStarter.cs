using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Development;

/// <summary>
/// Поднимает недостающую инфраструктуру из compose при старте сервиса на хосте.
/// </summary>
/// <remarks>
/// <para>
/// Зачем: при выключенном стенде приложение не падает сразу. Мигратор делает
/// попытки с паузами (в сумме около минуты), а брокер уходит в бесконечный
/// реконнект — на экране это выглядит как зависший F5. Дешевле поднять то, чего
/// не хватает, и сразу пойти дальше.
/// </para>
/// <para>
/// Исключений наружу не бросает: неудача — это предупреждение с хвостом вывода
/// compose. Сервис дальше упадёт на миграциях со своим сообщением, и это честнее,
/// чем уронить старт по причине «docker не найден».
/// </para>
/// <para>
/// Автостарт возможен ровно при трёх условиях сразу: флаг <c>Dev:AutoStartInfra</c>
/// включён, окружение — <c>Development</c> и в конфигурации есть локальные адреса.
/// Последнее отсекает контейнер: там профиль не подставляется, а адреса служб —
/// имена контейнеров, и поднимать compose изнутри контейнера нельзя.
/// </para>
/// </remarks>
public sealed class DevInfrastructureStarter : IDevInfrastructureStarter
{
    /// <summary>Запас поверх <c>--wait-timeout</c>: compose печатает отчёт уже после ожидания.</summary>
    private const int ComposeGraceSeconds = 60;

    /// <summary>Сколько строк хвоста вывода попадает в предупреждение.</summary>
    private const int TailLines = 10;

    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IDevEndpointProbe _probe;
    private readonly IDevProcessRunner _runner;
    private readonly ILogger<DevInfrastructureStarter> _logger;

    /// <summary>Собирает стартер: окружение, конфигурация, проба портов, раннер процесса.</summary>
    public DevInfrastructureStarter(
        IHostEnvironment environment,
        IConfiguration configuration,
        IDevEndpointProbe probe,
        IDevProcessRunner runner,
        ILogger<DevInfrastructureStarter> logger
    )
    {
        _environment = environment;
        _configuration = configuration;
        _probe = probe;
        _runner = runner;
        _logger = logger;
    }

    /// <inheritdoc />
    public void EnsureStarted()
    {
        try
        {
            var plan = Plan();

            if (plan is not null)
            {
                Start(plan);
            }
        }
        catch (Exception exception)
        {
            // Страховка на весь шаг: подсказка при отладке не стоит того, чтобы
            // стать причиной несостоявшегося запуска.
            _logger.LogWarning(
                exception,
                "Автостарт инфраструктуры не выполнен: {Message}",
                exception.Message
            );
        }
    }

    /// <summary>
    /// Что поднимать: <c>null</c> — нечего, причина уже записана в лог.
    /// </summary>
    private DevInfraPlan? Plan()
    {
        DevInfraPlan? plan = null;
        var options = ReadOptions();

        if (!options.AutoStartInfra)
        {
            _logger.LogDebug("Автостарт инфраструктуры выключен: Dev:AutoStartInfra не задан.");
        }
        else if (!_environment.IsDevelopment())
        {
            _logger.LogDebug(
                "Автостарт инфраструктуры пропущен: окружение {Environment} — не Development.",
                _environment.EnvironmentName
            );
        }
        else
        {
            plan = PlanLocalStart(options);
        }

        return plan;
    }

    /// <summary>
    /// Вторая половина решения — после флага и окружения: остались только
    /// локальные адреса, и по ним видно, что поднимать.
    /// </summary>
    private DevInfraPlan? PlanLocalStart(DevInfraOptions options)
    {
        DevInfraPlan? plan = null;

        var targets = DevInfrastructureTargetResolver.Resolve(_configuration);
        var local = targets.Where(target => target.IsLocal).ToArray();
        var silent = local.Where(target => !Listening(target)).ToArray();

        if (local.Length == 0)
        {
            _logger.LogDebug(
                "Автостарт инфраструктуры пропущен: в конфигурации нет локальных адресов — сервис, похоже, запущен в контейнере."
            );
        }
        else if (silent.Length == 0)
        {
            _logger.LogDebug(
                "Инфраструктура отвечает ({Addresses}) — docker не вызывается.",
                string.Join(", ", local.Select(target => target.Address))
            );
        }
        else
        {
            var located = Locate();

            if (located is not null)
            {
                plan = new DevInfraPlan(
                    located.Value.Root,
                    located.Value.EnvironmentFile,
                    PendingServices(options, targets, silent),
                    options.BackgroundServiceNames,
                    options.InfraWaitTimeoutSeconds
                );
            }
        }

        return plan;
    }

    /// <summary>
    /// Службы, которые надо поднять из блокирующего списка. Молчащая локальная
    /// цель — поднимать; цели нет вовсе — тоже поднимать (источника пробы нет, а
    /// пропущенная база ломает запуск, тогда как лишний <c>up</c> идемпотентен).
    /// Цель есть, но она не локальная — не наше дело: это чужая инфраструктура.
    /// </summary>
    private static string[] PendingServices(
        DevInfraOptions options,
        IReadOnlyList<DevInfrastructureTarget> targets,
        IReadOnlyList<DevInfrastructureTarget> silent
    )
    {
        var pending = options
            .ServiceNames.Where(service =>
            {
                var own = targets
                    .Where(target =>
                        string.Equals(target.Service, service, StringComparison.Ordinal)
                    )
                    .ToArray();
                var local = own.Where(target => target.IsLocal).ToArray();

                return own.Length == 0 || local.Any(target => silent.Contains(target));
            })
            .ToArray();

        return pending;
    }

    private bool Listening(DevInfrastructureTarget target)
    {
        var listening = _probe.Probe(target.Host, target.Port) == DevEndpointState.Listening;

        return listening;
    }

    /// <summary>
    /// Корень репозитория и файл окружения: <c>null</c> — поднимать нечем, и это
    /// уже сказано в логе.
    /// </summary>
    private (string Root, string EnvironmentFile)? Locate()
    {
        (string Root, string EnvironmentFile)? located = null;
        var root = FindRepositoryRoot();

        if (root is null)
        {
            _logger.LogWarning(
                "Автостарт инфраструктуры: от {ContentRoot} вверх не нашёл {Compose} — корень репозитория не найден, поднимите стенд вручную.",
                _environment.ContentRootPath,
                DevInfrastructureCompose.ComposeFile
            );
        }
        else
        {
            var environmentFile = Path.Combine(root, DevInfrastructureCompose.DevEnvironmentFile);

            if (File.Exists(environmentFile))
            {
                located = (root, environmentFile);
            }
            else
            {
                _logger.LogWarning(
                    "Автостарт инфраструктуры: нет {File} — поднимите стенд вручную (scripts/windows/stack.ps1 -Action Up -Dev или ./scripts/unix/stack.sh --action up --dev). Файл с секретами код не создаёт.",
                    DevInfrastructureCompose.DevEnvironmentFile
                );
            }
        }

        return located;
    }

    /// <summary>Поднимается вверх от каталога содержимого до каталога с compose-файлом.</summary>
    private string? FindRepositoryRoot()
    {
        string? root = null;
        DirectoryInfo? directory = string.IsNullOrWhiteSpace(_environment.ContentRootPath)
            ? null
            : new DirectoryInfo(_environment.ContentRootPath);

        while (directory is not null && root is null)
        {
            if (File.Exists(Path.Combine(directory.FullName, DevInfrastructureCompose.ComposeFile)))
            {
                root = directory.FullName;
            }

            directory = directory.Parent;
        }

        return root;
    }

    private DevInfraOptions ReadOptions()
    {
        var options = new DevInfraOptions();
        _configuration.GetSection(DevInfraOptions.SectionName).Bind(options);

        return options;
    }

    /// <summary>Две фазы: блокирующая — с ожиданием готовности, фоновая — без него.</summary>
    private void Start(DevInfraPlan plan)
    {
        WarnAboutMissingKeys(plan);

        var blocking = plan.Pending.Count == 0 || RunBlocking(plan);

        if (blocking)
        {
            RunBackground(plan);
        }
    }

    /// <summary>
    /// Отставший файл окружения — предупреждение, а не отказ: ключа может не быть
    /// у службы, которая сервису не нужна, и запрещать запуск из-за этого нельзя.
    /// </summary>
    private void WarnAboutMissingKeys(DevInfraPlan plan)
    {
        var directory = Path.GetDirectoryName(plan.EnvironmentFile) ?? string.Empty;
        var template = Path.Combine(directory, DevInfrastructureCompose.DevEnvironmentTemplateFile);
        var missing = EnvironmentFileParity.Missing(plan.EnvironmentFile, template);

        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "Файл окружения отстал от шаблона {Template}: нет {Keys}. Эти ключи compose подставит пустыми, и хостовый порт будет случайным — перенесите их в {File}.",
                DevInfrastructureCompose.DevEnvironmentTemplateFile,
                string.Join(", ", missing),
                DevInfrastructureCompose.DevEnvironmentFile
            );
        }
    }

    /// <summary>Блокирующая фаза: база и брокер, с ожиданием готовности.</summary>
    private bool RunBlocking(DevInfraPlan plan)
    {
        var result = RunCompose(plan, "база и брокер", plan.Pending, wait: true);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "База и брокер не поднялись ({Reason}): {Output}. Дальше сервис пойдёт своим ходом — миграции подождут базу сами.",
                result.Reason,
                Tail(result)
            );

            var hint = Hint(result);

            if (hint is not null)
            {
                _logger.LogWarning("{Hint}", hint);
            }
        }

        return result.Succeeded;
    }

    /// <summary>
    /// Фоновая фаза: наблюдаемость. Её отказ запуск не срывает — трейсы и панели
    /// можно не увидеть, работать сервис будет.
    /// </summary>
    private void RunBackground(DevInfraPlan plan)
    {
        if (plan.Background.Count > 0)
        {
            var result = RunCompose(plan, "наблюдаемость", plan.Background, wait: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Наблюдаемость не поднялась ({Reason}): {Output}. На работу сервиса это не влияет.",
                    result.Reason,
                    Tail(result)
                );
            }
        }
    }

    /// <summary>Один вызов compose; про успех сообщается строкой в лог, про отказ — вызывающий.</summary>
    private DevProcessResult RunCompose(
        DevInfraPlan plan,
        string phase,
        IReadOnlyList<string> services,
        bool wait
    )
    {
        var timeout = TimeSpan.FromSeconds(
            wait ? plan.WaitSeconds + ComposeGraceSeconds : plan.WaitSeconds
        );
        var request = new DevProcessRequest(
            DevInfrastructureCompose.Tool,
            DevInfrastructureCompose.UpArguments(services, wait, plan.WaitSeconds),
            plan.Root,
            timeout,
            wait
        );

        _logger.LogInformation(
            "Поднимаю {Phase} ({Services}): {Command}",
            phase,
            string.Join(", ", services),
            string.Join(" ", new[] { request.FileName }.Concat(request.Arguments))
        );

        var result = _runner.Run(request);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "{Phase} готова: {Services}.",
                phase,
                string.Join(", ", services)
            );
        }

        return result;
    }

    /// <summary>
    /// Что сделать читателю: самая частая причина отказа compose — погашенный
    /// Docker Desktop, вторая — его отсутствие в PATH. Оба случая распознаются по
    /// тексту, который compose и оболочка уже написали, и превращаются в прямое
    /// указание: иначе человек читает «cannot connect to the daemon» и ищет
    /// проблему в конфигурации стенда.
    /// </summary>
    private static string? Hint(DevProcessResult result)
    {
        string? hint = null;
        var text = string.Concat(
            result.StandardError,
            " ",
            result.StandardOutput,
            " ",
            result.Failure ?? string.Empty
        );

        if (result.Failure is not null)
        {
            hint = "docker не найден в PATH — проверьте, что Docker Desktop установлен.";
        }
        else if (
            text.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase)
            || text.Contains("docker daemon is not running", StringComparison.OrdinalIgnoreCase)
        )
        {
            hint = "Docker Desktop не запущен — запустите его и повторите.";
        }

        return hint;
    }

    /// <summary>
    /// Хвост вывода одной строкой: compose печатает и прогресс, и ошибку, а
    /// сообщение об отказе из-за этого уезжает вверх на экране.
    /// </summary>
    private static string Tail(DevProcessResult result)
    {
        var lines = string.Concat(result.StandardError, Environment.NewLine, result.StandardOutput)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .TakeLast(TailLines)
            .ToArray();

        var tail = lines.Length > 0 ? string.Join(" | ", lines) : "вывода нет";

        return tail;
    }

    /// <summary>Что и откуда поднимать: корень, файл окружения и два списка служб.</summary>
    private sealed record DevInfraPlan(
        string Root,
        string EnvironmentFile,
        IReadOnlyList<string> Pending,
        IReadOnlyList<string> Background,
        int WaitSeconds
    );
}
