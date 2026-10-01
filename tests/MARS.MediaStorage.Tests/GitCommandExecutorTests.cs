using MARS.MediaStorage.Services.Git;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 1: вызов git идёт через Process, поэтому критично, чтобы
/// результат команды возвращался структурой (код возврата + stdout + stderr),
/// а не бросал исключение. Иначе невозможно отличить «изменений нет»
/// (exit 0, пустой вывод) от реальной ошибки и корректно вести себя в CI.
/// Тесты выполняют настоящие git-вызовы против локального bare-репозитория.
/// </summary>
public class GitCommandExecutorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-git-exec-" + Guid.NewGuid().ToString("N")
    );

    private readonly GitCommandExecutor _executor = new();

    public GitCommandExecutorTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        GC.SuppressFinalize(this);
    }

    private string CreateBareRemote()
    {
        var remote = Path.Combine(_root, "remote.git");

        var init = _executor
            .RunAsync(_root, ["init", "--bare", "--initial-branch=master", remote])
            .GetAwaiter()
            .GetResult();

        Assert.True(init.Success, init.StandardError);

        return remote;
    }

    [Fact]
    public async Task RunAsync_OnSuccess_ReturnsZeroExitCode()
    {
        var remote = CreateBareRemote();

        var result = await _executor.RunAsync(_root, ["--version"]);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("git version", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_OnFailure_ReturnsExitCodeWithoutThrowing()
    {
        var result = await _executor.RunAsync(_root, ["rev-parse", "--verify", "no-such-ref"]);

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_WithMissingWorkingDirectory_FailsInsteadOfThrowing()
    {
        var missing = Path.Combine(_root, "does-not-exist");

        var result = await _executor.RunAsync(missing, ["status"]);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RunAsync_PassesStandardInput()
    {
        // Нужен для «git commit -F -» и безопасной передачи сообщений,
        // содержащих кавычки и переводы строк.
        var result = await _executor.RunAsync(_root, ["hash-object", "--stdin"], stdin: "payload");

        Assert.True(result.Success, result.StandardError);
        Assert.NotEmpty(result.StandardOutput.Trim());
    }

    [Fact]
    public async Task RunAsync_CapturesStandardErrorOnFailure()
    {
        var result = await _executor.RunAsync(_root, ["cat-file", "-p", "deadbeefdeadbeefdeadbeef"]);

        Assert.False(result.Success);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task RunAsync_ClonesAndReportsRemote()
    {
        var remote = CreateBareRemote();
        var clone = Path.Combine(_root, "clone");

        var result = await _executor.RunAsync(_root, ["clone", remote, clone]);

        Assert.True(result.Success, result.StandardError);
        Assert.True(Directory.Exists(Path.Combine(clone, ".git")));
    }
}
