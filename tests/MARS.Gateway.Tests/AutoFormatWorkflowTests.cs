namespace MARS.Gateway.Tests;

/// <summary>
/// Проверки workflow автоформатирования.
/// </summary>
/// <remarks>
/// Workflow работает в двух режимах: на PR — read-only проверка без переписывания
/// ветки; на main — разделение классов и CSharpier, после чего, если есть
/// изменения, открывается/обновляется отдельный PR <c>chore/auto-format</c> в
/// <c>main</c> (через PAT, чтобы required checks реально запустились). История
/// <c>main</c> не переписывается: вместо amend создаётся новый коммит в ветке
/// <c>chore/auto-format</c>.
/// </remarks>
public class AutoFormatWorkflowTests
{
    private static string ReadWorkflow() =>
        File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(".github", "workflows", "auto-format.yml")
        );

    /// <summary>
    /// Форматирование создаёт отдельный коммит в ветке <c>chore/auto-format</c>,
    /// а не amend'ит последний коммит <c>main</c>.
    /// </summary>
    [Fact]
    public void Formatting_creates_a_separate_pr_branch_commit()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("git commit -m", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("git commit --amend", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// Коммит автоформатирования принадлежит боту: имя и email берутся из
    /// <c>BOT_NAME</c>/<c>BOT_EMAIL</c>, а email — канонический адрес
    /// <c>github-actions[bot]</c>.
    /// </summary>
    [Fact]
    public void Auto_format_commit_is_authored_by_bot()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("git config user.name \"$BOT_NAME\"", workflow, StringComparison.Ordinal);
        Assert.Contains("git config user.email \"$BOT_EMAIL\"", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "41898282+github-actions[bot]@users.noreply.github.com",
            workflow,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Ветка <c>chore/auto-format</c> принадлежит боту, поэтому её можно
    /// force-push'ить: перезаписывается только она, а не <c>main</c>.
    /// </summary>
    [Fact]
    public void Auto_format_branch_is_force_pushed()
    {
        var commands = CommandLines(ReadWorkflow());

        Assert.Contains(
            commands,
            line => line.Contains("git push --force origin", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(
            commands,
            line => line.Contains("--force-with-lease", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// PR с автоформатированием создаётся или обновляется через <c>gh</c>, а не
    /// перезапуском workflow на amended-коммите.
    /// </summary>
    [Fact]
    public void Auto_format_pr_is_created_or_updated()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("gh pr create", workflow, StringComparison.Ordinal);
        Assert.Contains("gh pr view", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("gh workflow run ci.yml", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// Секрет PAT необязателен на уровне checkout-токена: используется
    /// <c>secrets.AUTO_FORMAT_TOKEN || github.token</c>.
    /// </summary>
    [Fact]
    public void Checkout_uses_pat_or_github_token()
    {
        var workflow = ReadWorkflow();

        Assert.Contains(
            "secrets.AUTO_FORMAT_TOKEN || github.token",
            workflow,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Только строки команд: комментарий о том, что force-push перезаписывает чужой
    /// коммит, должен остаться возможным.
    /// </summary>
    private static List<string> CommandLines(string workflow) =>
        [
            .. workflow
                .Split('\n')
                .Select(line => line.TrimEnd('\r').Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#')),
        ];
}
