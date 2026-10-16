using MARS.Shared.Development;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Командная строка compose для автостарта.
/// </summary>
/// <remarks>
/// Порядок важен и проверяется целиком: имя службы перед <c>-f</c> compose читает
/// как службу, а не как подкоманду, и вместо поднятия стенда получается
/// <c>unknown command</c>. Сверка с теми же строками в
/// <c>scripts/*/common</c> лежит в <c>MARS.Gateway.Tests</c>: там уже есть разбор
/// скриптов.
/// </remarks>
public class DevInfrastructureComposeTests
{
    [Fact]
    public void Ожидающая_фаза_собирает_флаги_и_службы_в_порядке_скриптов()
    {
        var arguments = DevInfrastructureCompose.UpArguments(
            ["postgres", "rabbitmq"],
            wait: true,
            waitTimeoutSeconds: 600
        );

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
            arguments
        );
    }

    [Fact]
    public void Фоновая_фаза_не_ждёт_готовности()
    {
        var arguments = DevInfrastructureCompose.UpArguments(
            ["tempo", "prometheus", "grafana", "loki", "alloy"],
            wait: false,
            waitTimeoutSeconds: 600
        );

        Assert.DoesNotContain("--wait", arguments);
        Assert.DoesNotContain("--wait-timeout", arguments);
        Assert.Equal("alloy", arguments[^1]);
    }

    /// <summary>
    /// Службы идут последними: пустой список должен дать команду без имён, но не
    /// потерять <c>up -d</c> — иначе compose получил бы «<c>up</c> без служб», то
    /// есть весь стенд.
    /// </summary>
    [Fact]
    public void Без_служб_команда_остаётся_командой()
    {
        var arguments = DevInfrastructureCompose.UpArguments(
            [],
            wait: false,
            waitTimeoutSeconds: 600
        );

        Assert.Equal(["up", "-d"], arguments.Skip(arguments.Count - 2));
    }

    /// <summary>
    /// Имена файлов и значение файла окружения — не литералы в стартере: их
    /// читают и тесты скриптов, поэтому они объявлены константами.
    /// </summary>
    [Fact]
    public void Имена_файлов_и_команда_заданы_константами()
    {
        Assert.Equal("docker", DevInfrastructureCompose.Tool);
        Assert.Equal("docker-compose.yml", DevInfrastructureCompose.ComposeFile);
        Assert.Equal("docker-compose.dev.yml", DevInfrastructureCompose.DevComposeFile);
        Assert.Equal(".env.development", DevInfrastructureCompose.DevEnvironmentFile);
        Assert.Equal(
            ".env.development.example",
            DevInfrastructureCompose.DevEnvironmentTemplateFile
        );
    }
}
