using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Development;

/// <summary>
/// Пишет файл цели Prometheus для сервиса, запущенного на хосте.
/// </summary>
/// <remarks>
/// <para>
/// Механизм — <c>file_sd_configs</c> у задачи <c>mars-local</c>: файл кладёт сам
/// процесс, а читает Prometheus. Список хостовых портов в конфиге был бы хуже по
/// двум причинам: в нём всегда были бы мёртвые цели, а при полностью
/// docker-стенде он опрашивал бы опубликованные порты контейнеров вторым разом.
/// </para>
/// <para>
/// Шаг живёт в коде, а не в <c>scripts/*/local</c>, поэтому F5 и
/// <c>local -Run</c> ведут себя одинаково и список целей не надо поддерживать
/// руками ни в одном из двух путей.
/// </para>
/// <para>
/// Лейбл <c>instance</c> у хостовой цели отличается от контейнерной
/// (<c>telegram-host</c> против <c>telegram</c>): панели дашборда фильтруют по
/// <c>instance</c>, и совпадение имён молча смешало бы отлаживаемый процесс с его
/// контейнером.
/// </para>
/// </remarks>
public sealed class DevPrometheusTargetRegistrar : IDevPrometheusTargetRegistrar
{
    /// <summary>Каталог целей относительно корня репозитория.</summary>
    /// <remarks>
    /// Тот же путь смонтирован в контейнер Prometheus только dev-оверрайдом: в
    /// боевом стенде хостовые процессы не появляются, а каталог был бы лишним.
    /// </remarks>
    public const string TargetsDirectory = "data/prometheus-local";

    /// <summary>Адрес хоста из контейнера: на Windows и macOS он есть сам.</summary>
    /// <remarks>
    /// На Linux имя даёт <c>extra_hosts: host.docker.internal:host-gateway</c> у
    /// контейнера Prometheus — тем же приёмом, что и AudioController ходит к
    /// микшеру Windows.
    /// </remarks>
    public const string HostAddress = "host.docker.internal";

    /// <summary>Ключ конфигурации с портом, который сервис объявляет целью.</summary>
    /// <remarks>
    /// Задаёт генератор профиля (<c>scripts/local_launch.py</c>) из строки
    /// <c>port=</c> файла данных. Второго источника правды не появляется: порт
    /// профиля и порт цели обязаны совпадать, иначе Prometheus опросил бы не тот
    /// адрес.
    /// </remarks>
    public const string MetricsPortKey = "Dev:MetricsPort";

    /// <summary>Суффикс инстанса хостового процесса.</summary>
    private const string InstanceSuffix = "-host";

    /// <summary>Лейбл, отличающий хостовую цель от контейнерной.</summary>
    private const string SourceLabel = "host";

    /// <summary>Нижний регистр в именах полей: так их ждёт Prometheus.</summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _serviceName;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevPrometheusTargetRegistrar> _logger;

    /// <summary>
    /// Собирает регистратор: имя проекта (то, что передаётся в
    /// <c>AddMarsDefaults</c>), конфигурация, окружение и логгер.
    /// </summary>
    /// <param name="serviceName">Имя проекта: MARS.TwitchCore.</param>
    /// <param name="configuration">Из неё читается <see cref="MetricsPortKey"/>.</param>
    /// <param name="environment">
    /// От него берётся каталог содержимого: корень репозитория ищется вверх от
    /// него, как и в автостарте инфраструктуры.
    /// </param>
    /// <param name="logger">Куда писать о цели и о причинах, по которым её нет.</param>
    public DevPrometheusTargetRegistrar(
        string serviceName,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DevPrometheusTargetRegistrar> logger
    )
    {
        _serviceName = serviceName;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Register()
    {
        try
        {
            Write();
        }
        catch (Exception exception)
        {
            // Подсказка по отладке не должна ронять запуск сервиса: без цели
            // Prometheus работает как раньше, а ронённый сервис не работает вовсе.
            _logger.LogWarning(
                exception,
                "Цель Prometheus для этого процесса не объявлена: {Message}",
                exception.Message
            );
        }
    }

    /// <summary>Путь файла цели и его содержимое — одной веткой, без ранних выходов.</summary>
    private void Write()
    {
        var written = false;
        var port = _configuration[MetricsPortKey];

        if (!_environment.IsDevelopment())
        {
            _logger.LogDebug(
                "Цель Prometheus не объявляется: окружение {Environment} — не хостовый запуск.",
                _environment.EnvironmentName
            );
        }
        else if (string.IsNullOrWhiteSpace(port))
        {
            // Ключ задаёт только профиль MARS.Local: в контейнере его нет, и там
            // цель объявлять нечего.
            _logger.LogDebug(
                "Цель Prometheus не объявляется: в конфигурации нет {Key} — сервис запущен не по профилю MARS.Local.",
                MetricsPortKey
            );
        }
        else if (!int.TryParse(port.Trim(), out var number) || number <= 0)
        {
            _logger.LogWarning(
                "{Key}={Port} — не порт. Цель Prometheus для этого процесса не объявлена.",
                MetricsPortKey,
                port
            );
        }
        else
        {
            written = Write(number);
        }
    }

    /// <summary>
    /// Записать файл цели. <c>false</c> — объявлять нечего: сервис не
    /// контейнеризуется или корень репозитория не найден.
    /// </summary>
    private bool Write(int port)
    {
        var written = false;
        var root = DevRepositoryRoot.Locate(_environment.ContentRootPath);
        var service = ComposeServiceNames.From(_serviceName);

        if (root is null)
        {
            _logger.LogWarning(
                "Цель Prometheus не объявлена: от {ContentRoot} вверх не нашлось {Compose} — корень репозитория не найден.",
                _environment.ContentRootPath,
                DevInfrastructureCompose.ComposeFile
            );
        }
        else if (service is null)
        {
            _logger.LogDebug(
                "Цель Prometheus не объявлена: по имени проекта {Service} не выводится имя службы compose.",
                _serviceName
            );
        }
        else if (!ComposeServices.Read(root).Contains(service))
        {
            // Такой сервис один: MARS.AudioController не контейнеризуется. Постоянно
            // «down» в списке целей выглядел бы как неисправность стенда.
            _logger.LogDebug(
                "Цель Prometheus не объявлена: службы {Service} в compose нет — сервис не контейнеризуется.",
                service
            );
        }
        else
        {
            var directory = Path.Combine(root, TargetsDirectory);

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"{_serviceName}.json");
            var entry = new ScrapeTarget(
                [$"{HostAddress}:{port}"],
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["instance"] = service + InstanceSuffix,
                    ["source"] = SourceLabel,
                }
            );

            // Массив, а не объект: file_sd ждёт JSON-массив целей. Одиночный
            // объект Prometheus молча не читает, и цель просто не появляется.
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(value: new[] { entry }, SerializerOptions)
            );

            _logger.LogInformation(
                "Prometheus опросит этот процесс по {Target} (задача mars-local, instance {Instance}).",
                $"{HostAddress}:{port}",
                service + InstanceSuffix
            );

            written = true;
        }

        return written;
    }

    /// <summary>Одна цель файла: адрес и лейблы, которые к ней приклеены.</summary>
    private sealed record ScrapeTarget(string[] Targets, Dictionary<string, string> Labels);
}
