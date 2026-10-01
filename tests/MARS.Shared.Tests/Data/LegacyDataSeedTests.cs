using MARS.Shared.Data;

namespace MARS.Shared.Tests.Data;

/// <summary>
/// <see cref="LegacyDataSeed"/> собирает SQL-текст, который уходит в PostgreSQL
/// как есть: ошибка в построителе всплывает не в тестах, а на развёртывании.
/// Дважды такая ошибка стоила реальных прогонов миграции — сперва
/// <c>RAISE EXCEPTION</c> без закрывающего <c>END IF;</c>, потом <c>INSERT
/// … SELECT</c> без <c>;</c>. Тесты фиксируют форму SQL, а не его смысл:
/// смысл проверяется интеграционно на живой базе.
/// </summary>
public class LegacyDataSeedTests
{
    private static LegacyDataSeed.TableCopy MinimalCopy(
        string? filter = null,
        int? expectedRows = null,
        string[]? uniqueSourceColumns = null
    )
    {
        return new LegacyDataSeed.TableCopy(
            StagingTable: "Legacy",
            TargetSchema: "svc",
            TargetTable: "Target",
            Columns:
            [
                LegacyDataSeed.Column("Id"),
                LegacyDataSeed.Column("Name")
            ],
            Filter: filter,
            ExpectedRows: expectedRows,
            UniqueSourceColumns: uniqueSourceColumns
        );
    }

    /// <summary>
    /// Приводит переводы строк к <c>\n</c>: <see cref="LegacyDataSeed"/>
    /// собирается через <c>StringBuilder.AppendLine</c>, то есть на Windows
    /// получит <c>\r\n</c>, и проверки формы не должны зависеть от ОС.
    /// </summary>
    private static string Normalize(string sql)
    {
        return sql.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <summary>Текст одного <c>INSERT … SELECT</c> — до ближайшего <c>END IF;</c>.</summary>
    private static string InsertStatementOf(string sql)
    {
        var start = sql.IndexOf("INSERT INTO", StringComparison.Ordinal);
        var end = sql.IndexOf("END IF;", start, StringComparison.Ordinal);
        return sql[start..end];
    }

    [Fact]
    public void Seed_SingleCopy_BalancesEveryBlock()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()], "Orphan"));

        Assert.Equal(1, CountOccurrences(sql, "DO $mars_seed$\nBEGIN\n"));
        Assert.Equal(1, CountOccurrences(sql, "END $mars_seed$;"));
        // Три вложенные конструкции: внешний guard на staging, guard на
        // заполненность целевой таблицы и проверка количества строк.
        Assert.Equal(3, CountToken(sql, "IF "));
        Assert.Equal(3, CountToken(sql, "END IF;"));
        // FOR в зачистке staging закрывается END LOOP.
        Assert.Equal(1, CountToken(sql, "FOR leftover IN"));
        Assert.Equal(1, CountToken(sql, "END LOOP;"));
    }

    [Fact]
    public void Seed_InsertSelect_IsTerminatedWithSemicolon()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.Contains("FROM \"public\".\"Legacy\"\n;\n", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_WithFilter_PutsWhereAfterFromSource()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy(filter: "\"Name\" = 'keep'")]));

        Assert.Contains("WHERE \"Name\" = 'keep'\n;", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_WithoutFilter_InsertHasNoWhere()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.DoesNotContain("WHERE", InsertStatementOf(sql), StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_ExpectedRows_ComparesAgainstFixedCountNotSource()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy(null, 7)]));

        Assert.Contains(
            "IF (SELECT count(*) FROM \"svc\".\"Target\") < 7 THEN",
            sql,
            StringComparison.Ordinal
        );
        // Сравнение с числом, а не с полным staging: у RootState строки делятся
        // фильтрами между сервисами, и сверка с полной таблицей отвергла бы
        // корректный перенос.
        Assert.DoesNotContain(
            "< (SELECT count(*) FROM \"public\".\"Legacy\")",
            sql,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Seed_WithoutExpectedRows_ComparesAgainstSourceCount()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.Contains(
            "IF (SELECT count(*) FROM \"svc\".\"Target\")"
                + " < (SELECT count(*) FROM \"public\".\"Legacy\") THEN",
            sql,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Seed_GuardsOnStagingRegclass_SoCleanDeployIsNoop()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.Contains(
            "IF to_regclass('public.\"Legacy\"') IS NOT NULL THEN",
            sql,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Seed_GuardsOnTargetEmptiness_SoRerunDoesNotDuplicate()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.Contains(
            "IF NOT EXISTS (SELECT 1 FROM \"svc\".\"Target\") THEN",
            sql,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Seed_MultipleCopiesOfSameStagingTable_AreAllImported()
    {
        var copies = new[]
        {
            new LegacyDataSeed.TableCopy(
                "Legacy",
                "a",
                "First",
                [LegacyDataSeed.Column("Id")],
                Filter: "\"Kind\" = 'first'"
            ),
            new LegacyDataSeed.TableCopy(
                "Legacy",
                "b",
                "Second",
                [LegacyDataSeed.Column("Id")],
                Filter: "\"Kind\" = 'second'"
            )
        };

        var sql = Normalize(LegacyDataSeed.Seed(copies));

        Assert.Equal(6, CountToken(sql, "IF "));
        Assert.Equal(6, CountToken(sql, "END IF;"));
        Assert.Contains("INSERT INTO \"a\".\"First\"", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO \"b\".\"Second\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_ColumnWithDifferentName_KeepsTargetQuotedAndSourceRaw()
    {
        var sql = Normalize(
            LegacyDataSeed.Seed(
                [
                    new LegacyDataSeed.TableCopy(
                        "Legacy",
                        "svc",
                        "Target",
                        [
                            LegacyDataSeed.Column("Id"),
                            LegacyDataSeed.Column("IsActive", "\"Active\"")
                        ]
                    )
                ]
            )
        );

        Assert.Contains("(\"Id\", \"IsActive\")", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT \"Id\", \"Active\"", sql, StringComparison.Ordinal);
        // Прошлая версия оборачивала и получателя в кавычки дважды, и текст
        // не разбирался сервером вообще.
        Assert.DoesNotContain("\"\"Id\"\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_Cleanup_DropsEveryStagingTableIncludingOrphans()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()], "DropMe", "DropMeToo"));

        Assert.Contains("'DropMe'", sql, StringComparison.Ordinal);
        Assert.Contains("'DropMeToo'", sql, StringComparison.Ordinal);
        Assert.Contains("'Legacy'", sql, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(sql, "'DropMe',"));
    }

    [Fact]
    public void Seed_Cleanup_RunsAfterTheRowCountChecks()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()], "DropMe"));

        // Зачистка необратима и потому допустима только после успешных проверок.
        Assert.True(
            sql.IndexOf("DROP TABLE IF EXISTS", StringComparison.Ordinal)
                > sql.IndexOf("END IF;", StringComparison.Ordinal),
            "staging должен удаляться после проверок количества"
        );
    }

    [Fact]
    public void Seed_WithoutCopies_EmitsNoCleanupBlock()
    {
        var sql = Normalize(LegacyDataSeed.Seed([]));

        Assert.DoesNotContain("DO $mars_seed_cleanup$", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_OrphansOnly_StillCleansStagingUp()
    {
        var sql = Normalize(LegacyDataSeed.Seed([], "DropMe"));

        Assert.Contains("DO $mars_seed_cleanup$", sql, StringComparison.Ordinal);
        Assert.Contains("'DropMe'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_UniqueColumns_GuardRunsBeforeInsert()
    {
        var sql = Normalize(
            LegacyDataSeed.Seed([MinimalCopy(null, null, ["SiteId"])])
        );

        var guard = sql.IndexOf("GROUP BY \"SiteId\" HAVING count(*) > 1", StringComparison.Ordinal);
        var insert = sql.IndexOf("INSERT INTO", StringComparison.Ordinal);

        Assert.True(guard >= 0, "ожидалась проверка дублей по SiteId");
        Assert.True(
            guard < insert,
            "дубли должны отвергаться до вставки, иначе строки запишутся наполовину"
        );
    }

    [Fact]
    public void Seed_UniqueColumns_CountGuardIncludesItsOwnEndIf()
    {
        var sql = Normalize(
            LegacyDataSeed.Seed([MinimalCopy(null, null, ["SiteId"])])
        );

        // guard на staging, guard на дубли, guard на заполненность, проверка
        // количества — четыре IF на одну копию.
        Assert.Equal(4, CountToken(sql, "IF "));
        Assert.Equal(4, CountToken(sql, "END IF;"));
    }

    [Fact]
    public void Seed_UniqueColumnsWithFilter_GroupsAfterWhere()
    {
        var sql = Normalize(
            LegacyDataSeed.Seed(
                [MinimalCopy("\"Kind\" = 'a'", null, ["SiteId"])]
            )
        );

        var where = sql.IndexOf("WHERE \"Kind\" = 'a'\n", StringComparison.Ordinal);
        var group = sql.IndexOf("GROUP BY \"SiteId\"", StringComparison.Ordinal);

        Assert.True(where >= 0 && where < group, "фильтр должен предшествовать GROUP BY");
    }

    [Fact]
    public void Seed_WithoutUniqueColumns_EmitsNoDuplicateGuard()
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy()]));

        Assert.DoesNotContain("HAVING count(*) > 1", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DateOrNull_ConvertsNegativeInfinityToNull()
    {
        Assert.Equal(
            "(CASE WHEN \"WhenAdded\" = '-infinity' THEN NULL ELSE \"WhenAdded\"::timestamptz END)",
            LegacyDataSeed.DateOrNull("\"WhenAdded\"")
        );
    }

    [Fact]
    public void TextTimestamp_KeepsNegativeInfinityForNotNullTargets()
    {
        // Целевая Fumos.WhenAdded объявлена NOT NULL, поэтому NULL сюда не годится.
        Assert.Equal("\"WhenAdded\"::timestamptz", LegacyDataSeed.TextTimestamp("\"WhenAdded\""));
    }

    /// <summary>
    /// Регрессия: <c>AppendLine</c> добавляет перевод строки, а не точку с
    /// запятой, поэтому <c>RAISE</c> в guard на дубли уезжал в базу без
    /// завершения. Текст при этом оставался идеально сбалансированным по
    /// IF/END IF, проверка баланса молчала, и миграция падала уже на
    /// развёртывании с «syntax error at or near END».
    /// </summary>
    [Fact]
    public void Seed_DuplicateGuard_RaiseIsTerminatedWithSemicolon()
    {
        var sql = LegacyDataSeed.Seed([MinimalCopy(null, null, ["SiteId"])]);

        Assert.Contains(
            "перенос невозможен';",
            sql,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// То же для <c>RAISE</c> с перечислением аргументов: последний аргумент
    /// должен закрываться точкой с запятой, иначе PL/pgSQL не увидит <c>END</c>.
    /// Проверяются обе ветви проверки количества.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(null)]
    public void Seed_RowCountRaise_LastArgumentIsTerminatedWithSemicolon(int? expectedRows)
    {
        var sql = LegacyDataSeed.Seed([MinimalCopy(null, expectedRows)]);

        // Регрессия: AppendLine ставит перевод строки, а не точку с запятой.
        // Без «;» на последнем аргументе PL/pgSQL не увидит END IF и миграция
        // упадёт уже на развёртывании, а не в тесте. Ветви различаются числом
        // аргументов, поэтому берём последнюю строку с count(*) после RAISE.
        var lines = Normalize(sql).Split('\n');
        var raise = Array.FindIndex(lines, l => l.Contains("RAISE EXCEPTION", StringComparison.Ordinal));

        Assert.True(
            raise >= 0,
            "проверка количества строк обязана сообщать о неполном переносе"
        );

        var argument = -1;

        for (var i = raise + 1; i < lines.Length; i++)
        {
            if (lines[i].Contains("count(*)", StringComparison.Ordinal))
            {
                argument = i;
            }
            else if (lines[i].Contains("END IF;", StringComparison.Ordinal))
            {
                break;
            }
        }

        Assert.True(
            argument > raise,
            "у RAISE должен быть хотя бы один аргумент с количеством строк"
        );
        Assert.EndsWith(");", lines[argument], StringComparison.Ordinal);
    }

    /// <summary>
    /// Полная проверка: каждая строка с отступом внутри PL/pgSQL-блока должна
    /// либо быть комментарием, либо открывать конструкцию (<c>… THEN</c>,
    /// <c>FOR …</c>), либо завершаться точкой с запятой. Список — по всем
    /// формам, которые реально порождает построитель.
    /// </summary>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("\"Kind\" = 'a'", null, null)]
    [InlineData(null, 7, null)]
    [InlineData(null, null, new[] { "SiteId" })]
    [InlineData("\"Kind\" = 'a'", 7, new[] { "SiteId", "ExternalId" })]
    public void Seed_EveryStatementInsidePlpgsqlBlock_IsTerminated(
        string? filter,
        int? expectedRows,
        string[]? uniqueColumns
    )
    {
        var sql = Normalize(LegacyDataSeed.Seed([MinimalCopy(filter, expectedRows, uniqueColumns)]));

        var continuation = false;

        foreach (var line in sql.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("DO ", StringComparison.Ordinal)
                || trimmed.StartsWith("END ", StringComparison.Ordinal)
                || trimmed.StartsWith("DECLARE", StringComparison.Ordinal)
                || trimmed.StartsWith("BEGIN", StringComparison.Ordinal))
            {
                continuation = false;
                continue;
            }

            if (trimmed.EndsWith(" THEN", StringComparison.Ordinal)
                || trimmed.StartsWith("FOR ", StringComparison.Ordinal))
            {
                continuation = true;
                continue;
            }

            if (continuation)
            {
                continuation = !trimmed.EndsWith(";", StringComparison.Ordinal);
                continue;
            }

            Assert.True(
                trimmed.EndsWith(";", StringComparison.Ordinal),
                $"строка не завершена точкой с запятой: «{trimmed}»"
            );
        }
    }

    /// <summary>
    /// Построитель обязан падать сам, а не отдавать в PostgreSQL текст, который
    /// там отвергнут. Проверяем на том же коде, который в проде давал
    /// «syntax error at or near END».
    /// </summary>
    [Fact]
    public void Seed_GeneratedSqlIsAcceptedByTheStatementTerminatorCheck()
    {
        var exception = Record.Exception(() =>
            LegacyDataSeed.Seed([MinimalCopy(null, 7, ["SiteId"])])
        );

        Assert.Null(exception);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>
    /// Считает строки, чей текст после отступа начинается с <paramref name="token"/>.
    /// Сравнение по началу строки нужно, чтобы не ловить вхождения внутри
    /// SQL-выражений и строковых литералов.
    /// </summary>
    private static int CountToken(string sql, string token)
    {
        return sql
            .Split('\n')
            .Count(line => line.TrimStart().StartsWith(token, StringComparison.Ordinal));
    }
}