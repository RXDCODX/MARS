namespace MARS.Gateway.Tests;

/// <summary>
/// Проверки workflow автоформатирования.
/// </summary>
/// <remarks>
/// Ловушка здесь в том, что оба варианта выглядят одинаково зелёными на прогоне.
/// Отдельный коммит поверх исходного и amend последнего коммита дают одинаковый
/// результат в репозитории через минуту — файл отформатирован, статус зелёный, —
/// но историю портят по-разному: первый добавляет запись, которой никто из людей
/// не писал, и прячет под ней последний коммит человека; второй переписывает его
/// на месте, оставляя автора прежним. Разница видна только в `git log`, а не в
/// результатах прогонов.
/// </remarks>
public class AutoFormatWorkflowTests
{
    private static string ReadWorkflow() =>
        File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(".github", "workflows", "auto-format.yml")
        );

    /// <summary>
    /// Форматирование ложится в последний коммит, а не добавляет свой.
    /// </summary>
    [Fact]
    public void Formatting_is_amended_into_the_last_commit()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("git commit --amend", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("git commit -m", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// Автор исходного коммита обязан уцелеть: amend с явным <c>--author</c>
    /// оставляет человека автором, а бот становится коммитером. Именно это и даёт
    /// «двух авторов при одном коммите»; <c>--reset-author</c> оставил бы в
    /// коммите только бота, и это уже не amend, а переписывание авторства.
    /// </summary>
    [Fact]
    public void Amend_keeps_the_original_author_and_makes_the_bot_a_committer()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("--author", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("--reset-author", workflow, StringComparison.Ordinal);
        Assert.Contains("Co-authored-by: %s <%s>", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "41898282+github-actions[bot]@users.noreply.github.com",
            workflow,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// После amend коммит другой, и обычный push упирается в «non-fast-forward».
    /// Форсировать историю можно только через <c>--force-with-lease</c>: голый
    /// <c>--force</c> перезаписал бы чужой коммит, уехавший вперёд, пока
    /// прогон форматировал.
    /// </summary>
    [Fact]
    public void Amended_history_is_pushed_with_force_with_lease_only()
    {
        var commands = CommandLines(ReadWorkflow());

        Assert.Contains(
            commands,
            line => line.Contains("--force-with-lease", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(
            commands,
            line =>
                line.Contains("--force", StringComparison.Ordinal)
                && !line.Contains("--force-with-lease", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Lease обязан быть привязан к SHA, который прогон выкачал, и ни к чему
    /// другому. Ошибка не теоретическая: на живом git в отдельном репозитории
    /// ожидаемое значение, взятое из <c>FETCH_HEAD</c> после <c>git fetch</c>
    /// (равно текущей голове ветки, только что обновлённой), всегда совпадало — и
    /// force-push проходил, хотя параллельный коммит человека в ветке лежал.
    /// Снесённый коммит узнаётся только в чужой ветке, поэтому ожидаемое
    /// значение обязано быть тем, с которого прогон начал.
    /// </summary>
    [Fact]
    public void Lease_is_pinned_to_the_sha_the_run_started_from()
    {
        var commands = CommandLines(ReadWorkflow());

        Assert.Contains(
            commands,
            line =>
                line.Contains("--force-with-lease=\"$ref:$START_SHA\"", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(
            commands,
            line => line.Contains("FETCH_HEAD", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Пуш сделанным токеном GitHub не запускает workflow: это защита от
    /// рекурсии. Значит после amend проверки на новом хеше не появятся сами, их
    /// надо запустить руками — иначе коммит попадёт в <c>main</c> непроверенным,
    /// а обязательные статусы так и останутся незакрытыми.
    /// </summary>
    [Fact]
    public void Checks_are_retriggered_for_the_amended_commit()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("gh workflow run ci.yml", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// С PAT из секрета пуш запускает проверки сам, и ручной перезапуск становится
    /// вторым прогоном того же кода. Поэтому секрет необязателен, а шаг
    /// перезапуска включается только когда PAT не задан.
    /// </summary>
    [Fact]
    public void Retrigger_runs_only_without_a_personal_access_token()
    {
        var workflow = ReadWorkflow();

        Assert.Contains(
            "secrets.AUTO_FORMAT_TOKEN || github.token",
            workflow,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Только строки команд: комментарий о том, что голый флаг перезаписывает чужой
    /// коммит, должен остаться возможным — иначе проверка заставила бы выбросить
    /// из workflow объяснение самого опасного места.
    /// </summary>
    private static List<string> CommandLines(string workflow) =>
        [
            .. workflow
                .Split('\n')
                .Select(line => line.TrimEnd('\r').Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#')),
        ];
}
