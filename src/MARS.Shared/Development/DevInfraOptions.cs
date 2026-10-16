namespace MARS.Shared.Development;

/// <summary>
/// Автостарт инфраструктуры при отладке сервиса на хосте (F5, профиль MARS.Local).
/// </summary>
/// <remarks>
/// <para>
/// Секция <c>Dev</c> есть только в профиле локального запуска: в
/// <c>appsettings*.json</c> её нет, поэтому в контейнерах автостарт выключен, а не
/// «почти выключен». Двойная гвардия (флаг и окружение) нужна потому, что стенд
/// разработки поднимается в контейнерах с
/// <c>ASPNETCORE_ENVIRONMENT=Development</c> — одного <c>IsDevelopment()</c> тут
/// мало.
/// </para>
/// <para>
/// Списки служб — имена служб <c>docker-compose.yml</c> через запятую: так же их
/// принимает <c>docker compose up</c>, так же они записаны в скриптах.
/// </para>
/// </remarks>
public sealed class DevInfraOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Dev";

    /// <summary>Включён ли автостарт. В профиле MARS.Local включается явно.</summary>
    public bool AutoStartInfra { get; set; }

    /// <summary>
    /// Службы, без которых сервис не стартует: поднимаются блокирующе
    /// (<c>up --wait</c>). Их отсутствие — отказ запуска: миграции ждут базу ~50 с
    /// и падают, а брокер пишет бесконечный реконнект.
    /// </summary>
    /// <remarks>
    /// Список — строкой, а не <c>string[]</c>: конфигурационный биндер не разбивает
    /// скалярный ключ на массив, и
    /// <c>Dev__InfraServices=postgres,rabbitmq</c> из профиля остался бы молча
    /// проигнорированным, то есть настройка жила бы только в дефолтах класса.
    /// </remarks>
    public string InfraServices { get; set; } = "postgres,rabbitmq";

    /// <summary>Имена блокирующих служб, разобранные из <see cref="InfraServices"/>.</summary>
    public string[] ServiceNames => Split(InfraServices);

    /// <summary>
    /// Службы наблюдаемости: поднимаются фоном, без ожидания готовности. Их отказ
    /// сервису не мешает — трейсы и панели можно не увидеть, но запуск это не
    /// срывает.
    /// </summary>
    public string InfraBackgroundServices { get; set; } = "tempo,prometheus,grafana,loki,alloy";

    /// <summary>Имена фоновых служб, разобранные из <see cref="InfraBackgroundServices"/>.</summary>
    public string[] BackgroundServiceNames => Split(InfraBackgroundServices);

    /// <summary>
    /// Сколько ждать готовности блокирующих служб. Значение уходит в
    /// <c>--wait-timeout</c>: на пустом томе postgres поднимается минуты, и
    /// короткий таймаут отдавал бы отказ раньше, чем база успела бы ответить.
    /// </summary>
    public int InfraWaitTimeoutSeconds { get; set; } = 600;

    /// <summary>Разбор списка через запятую: пустая строка — пустой список.</summary>
    private static string[] Split(string? value)
    {
        var names = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

        return names;
    }
}
