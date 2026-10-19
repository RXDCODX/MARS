namespace MARS.Shared.Development;

/// <summary>
/// Командная строка compose для поднятия инфраструктуры из хостового запуска.
/// </summary>
/// <remarks>
/// Имена файлов и порядок флагов повторяют <c>compose_args</c> в
/// <c>scripts/unix/common.sh</c> и <c>Get-MarsComposeArgument</c> в
/// <c>scripts/windows/common.ps1</c>: это один и тот же стенд, и два источника
/// строки не должны разойтись — сверку ведёт <c>ScriptsParityTests</c>.
///
/// Оба compose-файла берутся обязательно: dev-оверрайд — единственный, кто
/// публикует хостовые порты (<c>6017</c>, <c>6021</c>), то есть ровно те адреса,
/// которые пробует профиль MARS.Local. Один базовый файл поднял бы стенд без
/// портов снаружи, и сервис на хосте его бы не увидел.
/// </remarks>
public static class DevInfrastructureCompose
{
    /// <summary>Команда.</summary>
    public const string Tool = "docker";

    /// <summary>Базовый compose-файл.</summary>
    public const string ComposeFile = "docker-compose.yml";

    /// <summary>Dev-оверрайд: публикация хостовых портов и hot-reload.</summary>
    public const string DevComposeFile = "docker-compose.dev.yml";

    /// <summary>Файл окружения dev-стенда. Не создаётся и не правится кодом: там секреты владельца.</summary>
    public const string DevEnvironmentFile = ".env.development";

    /// <summary>Его шаблон — единственный источник ключей для сверки.</summary>
    public const string DevEnvironmentTemplateFile = ".env.development.example";

    /// <summary>
    /// Аргументы <c>docker compose … up -d</c>.
    /// </summary>
    /// <param name="services">Службы, которые поднимаем: без списка поднимается весь стенд.</param>
    /// <param name="wait">Ждать готовности по <c>service_healthy</c>.</param>
    /// <param name="waitTimeoutSeconds">Сколько ждать: уходит в <c>--wait-timeout</c>.</param>
    public static IReadOnlyList<string> UpArguments(
        IEnumerable<string> services,
        bool wait,
        int waitTimeoutSeconds
    )
    {
        var arguments = new List<string>
        {
            "compose",
            "--env-file",
            DevEnvironmentFile,
            "-f",
            ComposeFile,
            "-f",
            DevComposeFile,
            "up",
            "-d",
        };

        if (wait)
        {
            arguments.Add("--wait");
            arguments.Add("--wait-timeout");
            arguments.Add(
                waitTimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        arguments.AddRange(services);

        return arguments;
    }
}
