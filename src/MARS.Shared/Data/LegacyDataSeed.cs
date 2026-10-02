using System.Text;

namespace MARS.Shared.Data;

/// <summary>
/// Построитель импорта данных из временной staging-схемы <c>public</c> в схемы
/// сервиса.
///
/// <para>
/// Схема переезжала из одной базы <c>prod</c> в десять отдельных. Переносить данные
/// нужно один раз, а миграции EF применяются при каждом развёртывании — в том числе
/// там, где staging уже нет. Поэтому перенос оборачивается в
/// <c>to_regclass(...)</c>: если источника нет, блок не выполняется и миграция
/// проходит как пустая.
/// </para>
///
/// <para>
/// Одна staging-таблица может питать несколько целевых (например, <c>RootState</c>
/// расходится по пяти сервисам), поэтому проверка количества и удаление staging
/// выполняются один раз на всю партию, а не на каждую копию.
/// </para>
///
/// <para>
/// Порядок «импорт → проверка → удалить staging» выбран из соображений
/// восстанавливаемости: если перенос не довёл строки, миграция падает с
/// <c>RAISE EXCEPTION</c> и откатывается вместе со staging, так что данные
/// остаются доступными для разбора.
/// </para>
/// </summary>
public static class LegacyDataSeed
{
    /// <summary>Схема, в которую разворачивается дамп <c>pg_dump</c> перед переносом.</summary>
    public const string StagingSchema = "public";

    /// <summary>Отдельный долларовый тег: <c>$$</c> может встретиться в SQL-теле.</summary>
    private const string BlockTag = "$mars_seed$";

    /// <summary>
    /// Описание одной копии: какие выражения из staging класть в какие колонки.
    /// </summary>
    /// <param name="StagingTable">Таблица-источник в схеме <c>public</c>.</param>
    /// <param name="TargetSchema">Схема-получатель в этой базе.</param>
    /// <param name="TargetTable">Таблица-получатель.</param>
    /// <param name="Columns">
    /// Пары (колонка-получатель, выражение из источника) строго в порядке вставки.
    /// Перечисляются явно, а не <c>SELECT *</c>: иначе перестановка колонок в
    /// staging молча переставила бы значения в целевой таблице.
    /// </param>
    /// <param name="Filter">
    /// Дополнительное условие <c>WHERE</c>. Нужно там, где одну legacy-таблицу
    /// читают несколько сервисов и каждый получает свою часть строк.
    /// </param>
    /// <param name="ExpectedRows">
    /// Ожидаемое число строк, если в staging попали не все строки таблицы
    /// (например, <c>RootState</c> делится фильтрами между сервисами). При
    /// <c>null</c> строк не меньше, чем в источнике.
    /// </param>
    /// <param name="UniqueSourceColumns">
    /// Колонки источника, по которым в целевой таблице стоит уникальный индекс.
    /// Проверяются до вставки: без неё дубль отвергался бы уже на индексе,
    /// сообщение указывало бы на имя индекса, а не на исходные данные. С
    /// проверкой перенос падает до того, как что-либо запишет.
    /// </param>
    public sealed record TableCopy(
        string StagingTable,
        string TargetSchema,
        string TargetTable,
        IReadOnlyList<(string Target, string Source)> Columns,
        string? Filter = null,
        int? ExpectedRows = null,
        string[]? UniqueSourceColumns = null
    );

    /// <summary>
    /// SQL одной партии переносов: импорт пропущенных строк, проверка количества и
    /// удаление staging-таблиц. Одна транзакция на всю партию.
    /// </summary>
    /// <param name="copies">
    /// Копии в порядке применения: родительские таблицы раньше дочерних, иначе
    /// внешние ключи отвергнут вставку.
    /// </param>
    /// <param name="orphanStagingTables">
    /// Staging-таблицы без назначения. Удаляются, чтобы в базе сервиса не осталось
    /// мусора и случайная повторная обработка ничего не сломала.
    /// </param>
    public static string Seed(IEnumerable<TableCopy> copies, params string[] orphanStagingTables)
    {
        var copyList = copies.ToList();

        var result = new StringBuilder();

        result.AppendLine($"DO {BlockTag}");
        result.AppendLine("BEGIN");
        result.AppendLine("    -- Перенос выполняется только при наличии staging: на чистом");
        result.AppendLine("    -- развёртывании все проверки ниже дают false и блок пуст.");

        foreach (var copy in copyList)
        {
            AppendCopy(result, copy);
        }

        AppendLeftoverCleanup(result, copyList, orphanStagingTables);

        result.AppendLine($"END {BlockTag};");

        // Сгенерированный текст уходит в PostgreSQL как есть, поэтому
        // незакрытый IF всплыл бы как «syntax error at end of input» уже на
        // развёртывании. Считаем пары здесь: цена проверки нулевая, а
        // диагностика — с указанием строки.
        AssertBalanced(result.ToString());

        return result.ToString();
    }

    /// <summary>
    /// Проверяет, что каждый <c>IF … THEN</c> и каждый <c>FOR … LOOP</c> закрыт.
    /// Учитываются только строки, начинающиеся с отступа, — чтобы не считать
    /// <c>IF</c> внутри строковых литералов и SQL-выражений. Конструкции
    /// <c>BEGIN … END $tag$</c> игнорируются: они всегда парные по построению.
    /// </summary>
    private static void AssertBalanced(string sql)
    {
        var opened = 0;
        var closed = 0;

        using var reader = new StringReader(sql);

        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.TrimStart();

            if (
                trimmed.StartsWith("IF ", StringComparison.Ordinal)
                || trimmed.StartsWith("FOR ", StringComparison.Ordinal)
            )
            {
                opened++;
            }
            else if (
                trimmed.StartsWith("END IF;", StringComparison.Ordinal)
                || trimmed.StartsWith("END LOOP;", StringComparison.Ordinal)
            )
            {
                closed++;
            }
        }

        if (opened != closed)
        {
            throw new InvalidOperationException(
                $"Сгенерированный seed SQL не сбалансирован: открыто {opened}, закрыто {closed}."
                    + " Проверьте AppendCopy и AppendLeftoverCleanup."
            );
        }

        AssertStatementsTerminated(sql);
    }

    /// <summary>
    /// Проверяет, что каждый оператор внутри PL/pgSQL-блока закрыт точкой с запятой.
    /// </summary>
    /// <remarks>
    /// Баланс <c>IF</c>/<c>END IF</c> ничего не говорит о завершённости отдельных
    /// операторов: <c>RAISE EXCEPTION '…'</c> без <c>;</c> даёт идеально
    /// сбалансированный текст, который PostgreSQL отвергает с «syntax error at or
    /// near END» — то есть миграция падает уже на развёртывании. Заметили это на
    /// реальном PostgreSQL, поэтому проверка живёт здесь, а не в голове автора.
    ///
    /// <para>
    /// Пропускаются комментарии, строки со скобочными подстановками
    /// <c>(SELECT …)</c> в многострочном выражении и строки-разделители. Каждая
    /// строка с отступом внутри <c>DO</c>-блока должна быть либо комментарием,
    /// либо продолжением уже начатой конструкции, либо завершаться <c>;</c>.
    /// </para>
    /// </remarks>
    private static void AssertStatementsTerminated(string sql)
    {
        var continuation = false;

        using var reader = new StringReader(sql);

        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            // Комментарий не влияет на разбор и может быть многострочным.
            if (trimmed.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            // Метка DO ... / END $tag$; и DECLARE открывают и закрывают блоки
            // целиком, поэтому внутри них ничего не проверяем.
            if (
                trimmed.StartsWith("DO ", StringComparison.Ordinal)
                || trimmed.StartsWith("END ", StringComparison.Ordinal)
                || trimmed.StartsWith("DECLARE", StringComparison.Ordinal)
                || trimmed.StartsWith("BEGIN", StringComparison.Ordinal)
            )
            {
                continuation = false;
                continue;
            }

            // IF ... THEN, FOR ... IN, DECLARE x record и BEGIN открывают
            // конструкцию: тело пойдёт следующими строками, а закрывает их END.
            if (
                trimmed.EndsWith(" THEN", StringComparison.Ordinal)
                || trimmed.StartsWith("FOR ", StringComparison.Ordinal)
            )
            {
                continuation = true;
                continue;
            }

            if (continuation)
            {
                // Тело конструкции: строка закрывает себя точкой с запятой либо
                // продолжается (RAISE с перечислением аргументов).
                continuation = !trimmed.EndsWith(";", StringComparison.Ordinal);
                continue;
            }

            if (!trimmed.EndsWith(";", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Сгенерированный seed SQL содержит оператор без точки с запятой:"
                        + $" «{trimmed}». PostgreSQL отвергнет такой текст с"
                        + " \"syntax error at or near END\" уже при развёртывании."
                );
            }
        }
    }

    private static void AppendCopy(StringBuilder result, TableCopy copy)
    {
        var staging = Qualified(StagingSchema, copy.StagingTable);
        var target = Qualified(copy.TargetSchema, copy.TargetTable);

        var targetColumns = copy.Columns.Select(column => Quote(column.Target));
        var sourceExpressions = copy.Columns.Select(column => column.Source);

        result.AppendLine(
            $"    IF to_regclass('{StagingSchema}.{Quote(copy.StagingTable)}') IS NOT NULL THEN"
        );

        AppendDuplicateGuard(result, copy, staging);

        // Повторный запуск на уже заполненной таблице ничего не добавит: страховка
        // от ситуации «миграция применилась, staging развернули заново».
        result.AppendLine($"        IF NOT EXISTS (SELECT 1 FROM {target}) THEN");
        result.AppendLine($"            INSERT INTO {target}");
        result.AppendLine($"                ({string.Join(", ", targetColumns)})");
        result.AppendLine($"            SELECT {string.Join(", ", sourceExpressions)}");
        result.AppendLine($"            FROM {staging}");

        if (!string.IsNullOrWhiteSpace(copy.Filter))
        {
            result.AppendLine($"            WHERE {copy.Filter}");
        }

        result.AppendLine(";");
        result.AppendLine("        END IF;");

        // Молчаливая частичная загрузка хуже явного падения: пропущенные строки
        // обнаружились бы спустя месяцы, уже в проде.
        if (copy.ExpectedRows.HasValue)
        {
            result.AppendLine(
                $"        IF (SELECT count(*) FROM {target}) < {copy.ExpectedRows.Value} THEN"
            );
            result.AppendLine(
                $"            RAISE EXCEPTION"
                    + $" 'Неполный перенос {target}: ожидалось не меньше"
                    + $" {copy.ExpectedRows.Value} строк, получено %',"
            );
            result.AppendLine($"                (SELECT count(*) FROM {target});");
        }
        else
        {
            result.AppendLine(
                $"        IF (SELECT count(*) FROM {target})"
                    + $" < (SELECT count(*) FROM {staging}) THEN"
            );
            result.AppendLine(
                $"            RAISE EXCEPTION 'Неполный перенос {target}: в таблице % строк,"
                    + " в источнике % строк',"
            );
            result.AppendLine($"                (SELECT count(*) FROM {target}),");
            result.AppendLine($"                (SELECT count(*) FROM {staging});");
        }

        result.AppendLine("        END IF;");
        result.AppendLine("    END IF;");
        result.AppendLine();
    }

    /// <summary>
    /// Падение на дублях до вставки. Уникальный индекс отверг бы их и позже,
    /// но сообщение указывало бы на имя индекса, а не на то, какие строки в
    /// staging повторяются.
    /// </summary>
    private static void AppendDuplicateGuard(StringBuilder result, TableCopy copy, string staging)
    {
        if (copy.UniqueSourceColumns is not { Length: > 0 })
        {
            return;
        }

        var key = string.Join(", ", copy.UniqueSourceColumns.Select(Quote));

        result.AppendLine($"        IF EXISTS (SELECT 1 FROM {staging}");
        if (!string.IsNullOrWhiteSpace(copy.Filter))
        {
            result.AppendLine($"            WHERE {copy.Filter}");
        }

        result.AppendLine($"            GROUP BY {key} HAVING count(*) > 1) THEN");
        result.AppendLine(
            "            RAISE EXCEPTION"
                + $" 'Дубли в staging {copy.StagingTable}.{key}: перенос невозможен';"
        );
        result.AppendLine("        END IF;");
    }

    /// <summary>
    /// Финальная зачистка: удаляет все staging-таблицы этой партии, какие бы они
    /// ни остались. Вызывается последним, когда все проверки количества уже
    /// прошли, поэтому необратимое удаление происходит только при успешном переносе.
    /// </summary>
    private static void AppendLeftoverCleanup(
        StringBuilder result,
        IReadOnlyList<TableCopy> copies,
        IReadOnlyCollection<string> orphans
    )
    {
        var stagingTables = copies
            .Select(copy => copy.StagingTable)
            .Concat(orphans)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(table => table, StringComparer.Ordinal)
            .ToList();

        if (stagingTables.Count == 0)
        {
            return;
        }

        result.AppendLine("    -- Зачистка staging. Обход списком, а не набор DROP: таблица могла");
        result.AppendLine("    -- остаться, если её целевая копия была пропущена как уже");
        result.AppendLine("    -- выполненная. CASCADE снимает висящие ссылки на внешние ключи.");
        result.AppendLine("    DO $mars_seed_cleanup$");
        result.AppendLine("    DECLARE");
        result.AppendLine("        leftover record;");
        result.AppendLine("    BEGIN");
        result.AppendLine(
            "        FOR leftover IN SELECT c.oid::regclass AS name"
                + " FROM pg_class c"
                + " JOIN pg_namespace n ON n.oid = c.relnamespace"
                + $" WHERE n.nspname = '{StagingSchema}' AND c.relkind = 'r' AND c.relname IN ("
        );
        result.AppendLine(
            "            " + string.Join(", ", stagingTables.Select(table => $"'{table}'"))
        );
        result.AppendLine("        )");
        result.AppendLine("        LOOP");
        result.AppendLine(
            "            EXECUTE format('DROP TABLE IF EXISTS %s CASCADE', leftover.name);"
        );
        result.AppendLine("        END LOOP;");
        result.AppendLine("    END $mars_seed_cleanup$;");
    }

    /// <summary>
    /// Колонка переносится «как есть»: имя в получателе совпадает с именем в
    /// источнике. Имена без кавычек, экранирование — на стороне построителя.
    /// </summary>
    public static (string Target, string Source) Column(string name)
    {
        return (name, Quote(name));
    }

    /// <summary>
    /// Выражение кладётся в колонку получателя с другим именем. Нужен для
    /// переименований вроде <c>ServiceStates.IsActive → IsServiceActive</c> и для
    /// приведений типов. <paramref name="target"/> — имя без кавычек,
    /// <paramref name="source"/> — готовый SQL.
    /// </summary>
    public static (string Target, string Source) Column(string target, string source)
    {
        return (target, source);
    }

    /// <summary>Набор колонок с одинаковыми именами в источнике и получателе.</summary>
    public static (string Target, string Source)[] Columns(params string[] names)
    {
        return names.Select(name => Column(name)).ToArray();
    }

    /// <summary>Выражение, приводящее <c>-infinity</c> к <c>NULL</c>.</summary>
    /// <remarks>
    /// В legacy-таблицах даты хранились текстом, и незаполненное значение писалось
    /// как <c>-infinity</c>. PostgreSQL не приводит его к <c>timestamptz</c>
    /// неявно, а сохранять бесконечность в новой схеме незачем: «дата неизвестна»
    /// честнее выражается <c>NULL</c>.
    /// </remarks>
    public static string DateOrNull(string sourceExpression)
    {
        return "(CASE WHEN {0} = '-infinity' THEN NULL ELSE {0}::timestamptz END)".Replace(
            "{0}",
            sourceExpression,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Дата из текстовой legacy-колонки в <c>timestamptz</c> с сохранением
    /// <c>-infinity</c>.
    /// </summary>
    /// <remarks>
    /// Не путать с <see cref="DateOrNull"/>: там «дата неизвестна» честно
    /// выражается <c>NULL</c>, а здесь целевая колонка объявлена
    /// <c>NOT NULL</c> (в доменной модели это непустой <c>DateTime</c>), и
    /// подстановка <c>NULL</c> отвергла бы перенос. <c>-infinity</c> —
    /// валидное значение <c>timestamptz</c> и ровно то, что лежит в legacy.
    /// </remarks>
    public static string TextTimestamp(string sourceExpression)
    {
        return $"{sourceExpression}::timestamptz";
    }

    private static string Qualified(string schema, string table)
    {
        return $"{Quote(schema)}.{Quote(table)}";
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
