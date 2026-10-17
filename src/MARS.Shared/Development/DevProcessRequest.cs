namespace MARS.Shared.Development;

/// <summary>
/// Запрос на один внешний процесс.
/// </summary>
/// <param name="FileName">Что запускать: <c>docker</c>.</param>
/// <param name="Arguments">
/// Аргументы по одному, без склейки в строку: склейка ломается на путях со
/// пробелами, а <see cref="ProcessStartInfo.ArgumentList"/> экранирует их сам.
/// </param>
/// <param name="WorkingDirectory">Каталог запуска: корень репозитория, где лежат compose-файлы.</param>
/// <param name="Timeout">Предельное время жизни; <c>null</c> — ждать сколько угодно.</param>
/// <param name="StreamOutput">
/// Показывать ход процесса в консоль. Нужно блокирующей фазе: <c>--wait</c> идёт
/// минуты, и без вывода F5 выглядит как зависший.
/// </param>
public sealed record DevProcessRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null,
    bool StreamOutput = false
);
