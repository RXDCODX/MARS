using System.Diagnostics;
using System.Text;

namespace MARS.MediaStorage.Services.Git;

/// <summary>
/// Результат одной git-команды.
/// </summary>
/// <param name="ExitCode">Код возврата процесса.</param>
/// <param name="StandardOutput">stdout.</param>
/// <param name="StandardError">stderr.</param>
public readonly record struct GitCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError
)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Тонкая обёртка над вызовом git. Ошибки не бросаются: вызывающий код обязан
/// различать «изменений нет» (exit 0) и реальный сбой (ненулевой exit + stderr).
/// </summary>
public interface IGitCommandExecutor
{
    Task<GitCommandResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        string? stdin = null,
        CancellationToken cancellationToken = default
    );
}

public sealed class GitCommandExecutor : IGitCommandExecutor
{
    /// <summary>
    /// Базовые аргументы для каждого вызова. Без них git может подцепить
    /// пользовательский <c>~/.gitconfig</c> с credential.helper и prompt,
    /// из-за чего контейнер зависал бы на запросе пароля.
    /// </summary>
    private static readonly string[] BaseArguments =
    [
        "-c",
        "core.askPass=",
        "-c",
        "credential.helper=",
        "-c",
        "core.fsmonitor=false",
    ];

    public async Task<GitCommandResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        string? stdin = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!Directory.Exists(workingDirectory))
        {
            return new GitCommandResult(-1, string.Empty, $"Каталог не найден: {workingDirectory}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in BaseArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new GitCommandResult(-1, string.Empty, $"Не удалось запустить git: {ex.Message}");
        }

        // Потоки читаются только после старта процесса: до Start() обращение к
        // StandardOutput бросает InvalidOperationException.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        if (stdin is not null)
        {
            await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken);
        }

        process.StandardInput.Close();

        await process.WaitForExitAsync(cancellationToken);

        return new GitCommandResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask
        );
    }
}
