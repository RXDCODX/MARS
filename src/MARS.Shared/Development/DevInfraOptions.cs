namespace MARS.Shared.Development;

/// <summary>
/// Автостарт инфраструктуры при отладке сервиса на хосте (F5, профиль MARS.Local).
/// </summary>
/// <remarks>
/// <para>
/// Секция <c>Dev</c> есть только в профиле локального запуска: в
/// <c>appsettings*.json</c> её нет, поэтому в контейнерах автостарт останавливает
/// не флаг, а окружение с локальными адресами. Гвардия именно в этом порядке нужна
/// потому, что стенд разработки поднимается в контейнерах с
/// <c>ASPNETCORE_ENVIRONMENT=Development</c> — одного <c>IsDevelopment()</c> тут
/// мало.
/// </para>
/// <para>
/// Флаг по умолчанию включён (см. <see cref="AutoStartInfra"/>), потому что
/// профиль <c>MARS.Local</c> подставляет не всякая IDE, а без флага отладка на хосте
/// не поднимала бы инфраструктуру и не сказала бы почему.
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

    /// <summary>
    /// Включён ли автостарт.
    /// </summary>
    /// <remarks>
    /// Умолчание — <c>true</c>, и это не «автостарт везде»: решение принимает
    /// <see cref="DevInfrastructureStarter"/>, и первым он спрашивает окружение.
    /// Включено по умолчанию потому, что секции <c>Dev</c> может не быть вовсе —
    /// F5 в Visual Studio подхватит профиль с именем проекта, а не
    /// <c>MARS.Local</c>. Выключается явно: <c>Dev__AutoStartInfra=false</c>.
    /// </remarks>
    public bool AutoStartInfra { get; set; } = true;

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
