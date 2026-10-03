using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc.Telegramus;
using Xunit;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Контракт оверлейного хаба.
/// </summary>
/// <remarks>
/// <para>
/// Каждый метод <see cref="ITelegramusHub"/> обязан соответствовать ветке
/// <c>oneof</c> у <c>TelegramusEvent</c>. Иначе это не серверное событие, а
/// вызов клиента, и хаб его не обслуживает: <c>SignalR</c> ответит ошибкой на
/// <c>invoke</c>.
/// </para>
/// <para>
/// Проверка нужна из-за известного разрыва: клиент шлёт в хаб <c>MuteAll</c>,
/// <c>UnmuteSessions</c>, <c>ExplosionGo</c>, <c>MikuMikuDeleteTwitchMessages</c>,
/// <c>ObsFreeze</c>, <c>ObsUnfreeze</c>, <c>TwitchMsg</c> и <c>LogError</c>, а
/// методов с такими именами у хаба нет. <c>TwitchMsg</c> и <c>LogError</c>
/// существуют только как методы gRPC-сервиса <c>TelegramusGrpcService</c>, и
/// браузер до gRPC не ходит, так что через SignalR они уходят в пустоту.
/// </para>
/// <para>
/// Тест ловит обратную ситуацию — появление метода без ветки <c>oneof</c>. Если
/// кто-то добавит клиентский вызов в хаб, молча ушедший в пустоту, тест упадёт
/// с перечнем лишних имён.
/// </para>
/// </remarks>
public class OverlayHubContractTests
{
    /// <summary>
    /// Каждый метод хаба — это событие из <c>oneof</c>, а не вызов клиента.
    /// </summary>
    [Fact]
    public void Hub_methods_match_oneof_branches()
    {
        var hubMethods = typeof(ITelegramusHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var clientOnly = OneofBranchNames()
            .Where(name => !hubMethods.Contains(name))
            .Concat(hubMethods.Except(OneofBranchNames(), StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            clientOnly.Length == 0,
            "Расхождение контракта хаба с ветками oneof (вне oneof, но в хабе): "
                + string.Join(", ", clientOnly)
        );
    }

    /// <summary>
    /// Каждая ветка <c>oneof</c> обслуживается методом хаба.
    /// </summary>
    /// <remarks>
    /// Обратная сторона предыдущей проверки: событие, объявленное в proto, но не
    /// переложенное в хаб, приходит в gRPC, разбирается сервисом и молча
    /// теряется — на стенде это выглядит как «алерт не сработал».
    /// </remarks>
    [Fact]
    public void Oneof_branches_are_all_exposed_by_hub()
    {
        var hubMethods = typeof(ITelegramusHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = OneofBranchNames()
            .Where(name => !hubMethods.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Ветки oneof без метода хаба: " + string.Join(", ", missing)
        );
    }

    /// <summary>
    /// Имена методов хаба не совпадают с клиентскими вызовами, которых у хаба нет.
    /// </summary>
    /// <remarks>
    /// Список взят из карты вызовов клиента, а не из догадки. Проверка существует,
    /// чтобы известный разрыв не выглядел как «всё реализовано»: у хаба нет ни
    /// одного метода, который вызывал бы клиент.
    /// </remarks>
    [Fact]
    public void Hub_has_no_client_callable_methods()
    {
        var clientInvocations = new[]
        {
            "MuteAll",
            "UnmuteSessions",
            "ObsFreeze",
            "ObsUnfreeze",
            "ExplosionGo",
            "MikuMikuDeleteTwitchMessages",
            "TwitchMsg",
            "LogError",
        };

        var hubMethods = typeof(ITelegramusHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var unexpectedlyImplemented = clientInvocations
            .Where(hubMethods.Contains)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Если сервер когда-нибудь реализует один из этих вызовов, тест упадёт
        // и его уберут из списка в клиентской карте вместе с этим предупреждением
        // в HubAdapter: пока реализации нет, перечислять их как рабочие нельзя.
        Assert.True(
            unexpectedlyImplemented.Length == 0,
            "Клиентские вызовы, которых у хаба не должно быть: "
                + string.Join(", ", unexpectedlyImplemented)
        );
    }

    /// <summary>
    /// Каждый метод хаба есть в манифесте, по которому клиент строит карту типов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Звено, которого не было: <c>oneof</c> ↔ интерфейс проверял этот файл, а
    /// интерфейс ↔ манифест — только vitest-тест на стороне TypeScript, который
    /// манифест не читал. Добавили метод в <c>ITelegramusHub</c>, забыли про
    /// JSON — C#-тесты зелёные, а <c>useOverlayEvent</c> на новом событии не
    /// собирается.
    /// </para>
    /// <para>
    /// Манифест встроен в сборку ресурсом, поэтому сверка идёт с тем же файлом,
    /// который копируется в образ.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_hub_method_is_in_the_client_manifest()
    {
        var hubMethods = typeof(ITelegramusHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var manifestPath = Path.Combine(
            AppContext.BaseDirectory,
            "Hubs",
            "overlay-hub.manifest.json"
        );

        Assert.True(File.Exists(manifestPath), $"Манифест не найден: {manifestPath}");

        var manifestNames =
            JsonSerializer.Deserialize<List<string>>(File.ReadAllText(manifestPath)) ?? [];

        var missing = manifestNames
            .Except(hubMethods, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "В манифесте есть события, которых нет в хабе: " + string.Join(", ", missing)
        );
    }

    private static string[] OneofBranchNames() =>
        Enum.GetNames(typeof(TelegramusEvent.EventOneofCase))
            .Where(name => !name.StartsWith("None", StringComparison.Ordinal))
            .Select(StripOneofSuffix)
            .ToArray();

    private static string StripOneofSuffix(string name) =>
        name.EndsWith("Case", StringComparison.Ordinal) ? name[..^"Case".Length] : name;
}
