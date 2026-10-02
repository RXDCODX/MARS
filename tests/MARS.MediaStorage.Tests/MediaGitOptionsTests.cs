using MARS.MediaStorage.Services.Git;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 1: конфигурация git-синхронизации. Подсистема обязана быть
/// выключена по умолчанию, иначе сервис при старте попытался бы клонировать
/// внешний репозиторий в рабочий каталог и утёк токен в <c>.git/config</c>.
/// </summary>
public class MediaGitOptionsTests
{
    [Fact]
    public void Defaults_AreDisabled()
    {
        var options = new MediaGitOptions();

        Assert.False(options.Enabled);
    }

    [Fact]
    public void Defaults_UseSafeValues()
    {
        // Аудит Stage 1: готовность репозитория определяется наличием .git на
        // диске, а не флагом в конфигурации — флаг мог бы разъехаться с
        // реальностью после восстановления тома. Значения по умолчанию обязаны
        // быть неопасными.
        var options = new MediaGitOptions();

        Assert.Equal("master", options.Branch);
        Assert.Equal("origin", options.RemoteName);
        Assert.Null(options.Token);
        Assert.True(options.TimeoutSeconds > 0);
    }

    [Fact]
    public void Validate_WhenDisabled_ReturnsNoProblems()
    {
        var options = new MediaGitOptions { Enabled = false, RepositoryUrl = "" };

        Assert.Empty(options.Validate());
    }

    [Fact]
    public void Validate_WhenEnabledWithoutRepositoryUrl_ReportsProblem()
    {
        var options = new MediaGitOptions { Enabled = true, RepositoryUrl = "   " };

        var problems = options.Validate();

        Assert.Contains(
            problems,
            p => p.Contains("RepositoryUrl", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenEnabledWithBlankBranch_ReportsProblem(string branch)
    {
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "https://github.com/RXDCODX/random-memes.git",
            Branch = branch,
        };

        var problems = options.Validate();

        Assert.Contains(problems, p => p.Contains("Branch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenEnabledWithBlankAuthor_ReportsProblem()
    {
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "https://github.com/RXDCODX/random-memes.git",
            Branch = "master",
            AuthorName = "",
            AuthorEmail = "",
        };

        var problems = options.Validate();

        Assert.Contains(
            problems,
            p => p.Contains("AuthorName", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(
            problems,
            p => p.Contains("AuthorEmail", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Validate_WhenEnabledAndComplete_ReturnsNoProblems()
    {
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "https://github.com/RXDCODX/random-memes.git",
            Branch = "master",
        };

        Assert.Empty(options.Validate());
    }

    [Fact]
    public void Validate_RejectsNonHttpRepositoryUrl()
    {
        // file:// и ssh допустимы для локальных зеркал, а вот произвольные
        // схемы (ext::, --upload-pack) не должны попадать в ProcessStartInfo.
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "ext::sh -c malicious",
            Branch = "master",
        };

        Assert.NotEmpty(options.Validate());
    }
}
