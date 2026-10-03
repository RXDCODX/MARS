using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc.Telegramus;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Контракт оверлейного хаба хранится не в коде, а в <c>overlay-hub.manifest.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Имя метода хаба — это строка на проводе: клиент подписывается на неё строкой,
/// и компилятор не проверяет совпадение. Именно на этом держались расхождения
/// <c>updatewaifuprizes</c> / <c>UpdateWaifuPrizes</c> и <c>TunaMusicInfo</c> /
/// <c>SendPlayerData</c> в клиенте монолита: обе стороны компилировались, обе
/// были «правильными», а события просто не доходили.
/// </para>
/// <para>
/// Манифест нужен, чтобы расхождение ловилось падающим тестом, а не вниманием.
/// Тест закрывает обе стороны: <see cref="Manifest_matches_proto_event_cases"/>
/// сверяет его с ожидаемым списком имён, а
/// <see cref="Manifest_matches_hub_interface"/> — с реальным интерфейсом. Клиентский
/// vitest-тест сверяет с ним же свой список ключей.
/// </para>
/// </remarks>
public class OverlayHubManifestTests
{
    /// <summary>
    /// Ожидаемые имена методов. Список взят из веток <c>oneof event</c> в
    /// <c>telegramus.proto</c>: <see cref="TelegramusEvent"/> приходит по
    /// подписке одной оболочкой, и реле раскладывает её по этим именам.
    /// </summary>
    private static readonly string[] ExpectedEventNames =
    [
        "AddNewWaifu",
        "Adhd",
        "AdhdConfig",
        "Alert",
        "Alerts",
        "AllRefund",
        "AudioQuizStart",
        "AudioQuizStop",
        "AutoMessage",
        "Credits",
        "DeleteMessage",
        "Explosion",
        "FrogRoll",
        "FumoFriday",
        "FumoRoll",
        "GaoAlert",
        "Highlite",
        "LeroyAlert",
        "MakeScreenEmojisParticles",
        "MakeScreenParticles",
        "MergeWaifu",
        "MichaelJackson",
        "MikuMikuBeam",
        "MikuMonday",
        "MikuRoll",
        "NewMessage",
        "PhonkEdit",
        "PostTwitchInfo",
        "RandomMem",
        "ShowCurrentWife",
        "TikTokEdit",
        "UpdateFrogPrizes",
        "UpdateFumoPrizes",
        "UpdateMikuPrizes",
        "UpdateWaifuPrizes",
        "WaifuRoll",
    ];

    /// <summary>
    /// Логическое имя встроенного ресурса: <c>RootNamespace</c> сборки плюс
    /// путь файла точками. Задаётся явно, потому что при смене папки
    /// <c>Hubs</c> имя по умолчанию поедет, и тест упадёт с
    /// <c>ManifestResourceNotFoundException</c> вместо внятного сообщения.
    /// </summary>
    private const string ManifestResourceName = "MARS.Alerts.Hubs.overlay-hub.manifest.json";

    private static string[] ReadManifest()
    {
        using var stream = typeof(ITelegramusHub).Assembly.GetManifestResourceStream(
            ManifestResourceName
        );

        Assert.True(stream is not null, $"Ресурс не найден: {ManifestResourceName}");

        using var reader = new StreamReader(stream!);
        var names = JsonSerializer.Deserialize<string[]>(reader.ReadToEnd());

        Assert.NotNull(names);

        return names!;
    }

    /// <summary>
    /// Имена методов интерфейса вместе с унаследованными.
    /// </summary>
    /// <remarks>
    /// <see cref="Type.GetMethods()"/> на интерфейсе не разворачивает
    /// наследование: для <c>IOverlayHub</c> вернул бы пустой набор, и проверка
    /// прошла бы на пустом сравнении. Обход базовых интерфейсов сделан явно.
    /// </remarks>
    private static string[] ReadInterfaceMethods(Type type) =>
        type.GetInterfaces()
            .Append(type)
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void Manifest_exists()
    {
        var names = ReadManifest();

        Assert.NotEmpty(names);
    }

    [Fact]
    public void Manifest_matches_proto_event_cases()
    {
        var names = ReadManifest().OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(ExpectedEventNames, names);
    }

    [Fact]
    public void Manifest_matches_hub_interface()
    {
        var manifest = ReadManifest().OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var methods = ReadInterfaceMethods(typeof(ITelegramusHub));

        Assert.Equal(methods, manifest);
    }

    /// <summary>
    /// Каждому событию из <c>oneof</c> соответствует ровно один метод хаба.
    /// Проверка на количество ловит забытое событие: лишний метод и лишний
    /// ключ проходят по спискам одинаково, но теряют событие по дороге.
    /// </summary>
    [Fact]
    public void Manifest_covers_every_proto_event()
    {
        var cases = Enum.GetValues<TelegramusEvent.EventOneofCase>()
            .Where(c => c != TelegramusEvent.EventOneofCase.None)
            .ToArray();

        Assert.Equal(ExpectedEventNames.Length, cases.Length);
    }

    /// <summary>
    /// Пустых событий в <c>oneof</c> шесть, и клиенту нечего в них передавать:
    /// <c>new EmptyEvent()</c> не несёт информации. Их методы не принимают
    /// аргументов — иначе реле пришлось бы конструировать пустой объект ради
    /// вызова, который клиент всё равно игнорирует.
    /// </summary>
    [Fact]
    public void Empty_events_have_no_parameters()
    {
        var expectedWithoutPayload = new[]
        {
            nameof(ITelegramusHub.AudioQuizStop),
            nameof(ITelegramusHub.Credits),
            nameof(ITelegramusHub.Explosion),
            nameof(ITelegramusHub.LeroyAlert),
            nameof(ITelegramusHub.MichaelJackson),
            nameof(ITelegramusHub.PhonkEdit),
        };

        var withPayload = typeof(ITelegramusHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetParameters().Length != 1)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedWithoutPayload, withPayload);
    }

    /// <summary>
    /// <see cref="IOverlayHub"/> наследует <see cref="ITelegramusHub"/>, поэтому
    /// <c>Hub&lt;IOverlayHub&gt;</c> отдаёт клиенту все методы без дублирования
    /// объявлений. Если объявить их ещё раз в наследнике, они продублируются в
    /// списке методов интерфейса — и проверка числа методов это заметит.
    /// </summary>
    [Fact]
    public void Overlay_hub_inherits_telegramus_methods_without_duplicates()
    {
        var declaredByOverlay = typeof(IOverlayHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToArray();

        // Наследник не переобъявляет методы: повторное объявление создало бы
        // второй путь к тому же контракту, и расхождение между ними было бы
        // не видно ни компилятору, ни тесту.
        Assert.Empty(declaredByOverlay);
        Assert.Equal(
            ReadInterfaceMethods(typeof(ITelegramusHub)),
            ReadInterfaceMethods(typeof(IOverlayHub))
        );
    }
}
