using System.Diagnostics;
using MARS.Shared.Development;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Раннер внешнего процесса.
/// </summary>
/// <remarks>
/// Проверяется настоящим процессом, а не заглушкой: предмет проверки — чтение
/// stdout и stderr, код возврата и остановка по сроку, то есть ровно то, что
/// заглушка изображает, а не проверяет. Команды различаются по платформе: тесты
/// идут и в CI на Linux, где <c>cmd</c> нет.
/// </remarks>
public class DevProcessRunnerTests
{
    private readonly DevProcessRunner _runner = new();

    /// <summary>Долгая команда: заведомо дольше срока, который даёт тест.</summary>
    private static readonly string SleepScript = OperatingSystem.IsWindows()
        ? "ping -n 60 127.0.0.1"
        : "sleep 60";

    /// <summary>Команда оболочки текущей платформы.</summary>
    private static DevProcessRequest Command(
        string script,
        TimeSpan? timeout = null,
        bool stream = false
    ) =>
        OperatingSystem.IsWindows()
            ? new DevProcessRequest(
                "cmd",
                ["/c", script],
                AppContext.BaseDirectory,
                timeout,
                stream
            )
            : new DevProcessRequest(
                "/bin/sh",
                ["-c", script],
                AppContext.BaseDirectory,
                timeout,
                stream
            );

    /// <summary>Команда, которая пишет в stderr и возвращает заданный код.</summary>
    private static DevProcessRequest Failing(string text, int code) =>
        Command(
            OperatingSystem.IsWindows()
                ? $"echo {text} 1>&2 & exit /b {code}"
                : $"echo {text} >&2; exit {code}"
        );

    [Fact]
    public void Успешный_процесс_отдаёт_код_ноль_и_вывод()
    {
        var result = _runner.Run(Command("echo mars-ok"));

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("mars-ok", result.StandardOutput);
        Assert.False(result.TimedOut);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void Ненулевой_код_и_stderr_видны_в_результате()
    {
        var result = _runner.Run(Failing("mars-oops", 3));

        Assert.False(result.Succeeded);
        Assert.Equal(3, result.ExitCode);
        Assert.Contains("mars-oops", result.StandardError);
        Assert.Contains("код возврата 3", result.Reason);
    }

    /// <summary>
    /// Отсутствующая команда — это <see cref="DevProcessResult.Failure"/>, а не
    /// исключение: <c>docker</c> может не стоять в PATH, и это состояние машины, а
    /// не ошибка сервиса.
    /// </summary>
    [Fact]
    public void Отсутствующая_команда_попадает_в_причину()
    {
        var result = _runner.Run(
            new DevProcessRequest(
                "mars-missing-command-for-tests",
                ["--version"],
                AppContext.BaseDirectory
            )
        );

        Assert.False(result.Succeeded);
        Assert.Equal(-1, result.ExitCode);
        Assert.NotNull(result.Failure);
        Assert.Contains("mars-missing-command-for-tests", result.Reason);
    }

    /// <summary>
    /// Срок истёк — процесс остановлен, а тест не ждёт <c>sleep 60</c>: иначе
    /// проверка сама стала бы минутной.
    /// </summary>
    [Fact]
    public void Превышение_срока_останавливает_процесс()
    {
        var started = Stopwatch.StartNew();
        var result = _runner.Run(Command(SleepScript, TimeSpan.FromMilliseconds(400)));
        started.Stop();

        Assert.True(result.TimedOut);
        Assert.False(result.Succeeded);
        Assert.Contains("срок", result.Reason);
        Assert.True(
            started.Elapsed < TimeSpan.FromSeconds(30),
            $"Ожидание превысило срок и затянулось на {started.Elapsed.TotalSeconds:0.0} с."
        );
    }

    /// <summary>Вывод в консоль — только показ хода: результат от него не меняется.</summary>
    [Fact]
    public void Вывод_в_консоль_не_меняет_результат()
    {
        var result = _runner.Run(Command("echo mars-stream", stream: true));

        Assert.True(result.Succeeded);
        Assert.Contains("mars-stream", result.StandardOutput);
    }
}
