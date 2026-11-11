using System.Diagnostics;
using System.Text;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Test-only helper for preparing and inspecting local bare Git repositories.
/// Production Git operations are performed through LibGit2Sharp.
/// </summary>
internal sealed class GitTestCommandExecutor
{
    private static readonly string[] BaseArguments =
    [
        "-c",
        "core.askPass=",
        "-c",
        "credential.helper=",
        "-c",
        "core.fsmonitor=false",
    ];

    public async Task<GitTestCommandResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        string? stdin = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environmentVariables = null
    )
    {
        if (!Directory.Exists(workingDirectory))
        {
            return new GitTestCommandResult(
                -1,
                string.Empty,
                $"Каталог не найден: {workingDirectory}"
            );
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

        // Test repositories can live in temporary directories with different
        // ownership in CI containers. Allow only the exact directory under test.
        startInfo.ArgumentList.Add("-c");
        var safeDirectory = Path.GetFullPath(workingDirectory).Replace('\\', '/');
        startInfo.ArgumentList.Add($"safe.directory={safeDirectory}");

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new GitTestCommandResult(-1, string.Empty, ex.Message);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        if (stdin is not null)
        {
            await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken);
        }

        process.StandardInput.Close();
        await process.WaitForExitAsync(cancellationToken);

        return new GitTestCommandResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask
        );
    }
}

internal readonly record struct GitTestCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError
)
{
    public bool Success => ExitCode == 0;
}
