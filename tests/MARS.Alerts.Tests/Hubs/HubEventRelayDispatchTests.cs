using System.Text.Json;
using MARS.Alerts.Hubs;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Реле переводит оболочку <c>TelegramusEvent</c> в вызов метода хаба.
/// </summary>
/// <remarks>
/// <para>
/// Проверяется не «хоть какой-то метод вызван», а полнота и точность раскладки.
/// Событие без ветки в <c>switch</c> дошло бы до оверлея молча, а это ровно тот
/// класс отказа, ради которого имена вынесены в манифест.
/// </para>
/// <para>
/// Ожидаемое имя берётся из того же манифеста, что и контракт хаба, а не из
/// списка в тесте: иначе проверка повторяла бы правду реле и пропускала бы
/// забытое событие вместе с ним. Порядок веток <c>oneof</c> совпадает с
/// порядком манифеста — оба следуют номерам полей в <c>telegramus.proto</c>.
/// </para>
/// </remarks>
public class HubEventRelayDispatchTests
{
    private const string ManifestResourceName = "MARS.Alerts.Hubs.overlay-hub.manifest.json";

    private static string[] ReadManifest()
    {
        using var stream = typeof(IOverlayHub).Assembly.GetManifestResourceStream(
            ManifestResourceName
        );

        Assert.True(stream is not null, $"Ресурс не найден: {ManifestResourceName}");

        using var reader = new StreamReader(stream!);

        return JsonSerializer.Deserialize<string[]>(reader.ReadToEnd())!;
    }

    /// <summary>
    /// Ветки <c>oneof</c> в порядке объявления. <c>None</c> — не событие, а
    /// признак пустой оболочки, и в нумерации полей он не участвует.
    /// </summary>
    private static TelegramusEvent.EventOneofCase[] EventCases() =>
        Enum.GetValues<TelegramusEvent.EventOneofCase>()
            .Where(v => v != TelegramusEvent.EventOneofCase.None)
            .ToArray();

    /// <summary>
    /// Реле с заглушкой транспорта. Проверяется <see cref="IClientProxy"/>:
    /// типизированных методов у <c>IHubContext</c> нет, и имя на проводе —
    /// это первый аргумент <c>SendCoreAsync</c>.
    /// </summary>
    private static (Mock<IClientProxy> Proxy, HubEventRelay Relay) BuildRelay()
    {
        var proxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);

        var context = new Mock<IHubContext<OverlayHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);

        return (
            proxy,
            new HubEventRelay(new GrpcEventBroadcaster<TelegramusEvent>(), context.Object)
        );
    }

    private static string[] SentMethodNames(Mock<IClientProxy> proxy) =>
        proxy
            .Invocations.Where(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync))
            .Select(i => (string)i.Arguments[0]!)
            .ToArray();

    /// <summary>
    /// На каждую ветку <c>oneof</c> — ровно один вызов, и имя метода совпадает
    /// с манифестом. Проверка на «ровно один» ловит и забытое событие, и
    /// сработавшее не то, а сравнение имён ловит переставленные местами.
    /// </summary>
    [Fact]
    public async Task Dispatch_routes_every_event_case_to_its_hub_method()
    {
        var manifest = ReadManifest();
        var cases = EventCases();

        Assert.Equal(cases.Length, manifest.Length);

        for (var index = 0; index < cases.Length; index++)
        {
            var (proxy, relay) = BuildRelay();

            await relay.DispatchAsync(
                SampleFor(cases[index]),
                TestContext.Current.CancellationToken
            );

            Assert.Equal([manifest[index]], SentMethodNames(proxy));
        }
    }

    /// <summary>
    /// Методы веток с <c>EmptyEvent</c> не принимают аргументов, поэтому на
    /// проводу уходит пустой список. Если бы ушёл пустой объект, клиент получил
    /// бы лишний аргумент, а тип подписи перестал бы соответствовать манифесту.
    /// </summary>
    [Fact]
    public async Task Dispatch_sends_no_arguments_for_empty_events()
    {
        var (proxy, relay) = BuildRelay();

        await relay.DispatchAsync(
            new TelegramusEvent { Credits = new EmptyEvent() },
            TestContext.Current.CancellationToken
        );

        var call = Assert.Single(proxy.Invocations);
        Assert.Empty((object?[])call.Arguments[1]!);
    }

    /// <summary>
    /// Пустой <c>oneof</c> приходит только от клиента, собравшего сообщение
    /// вручную. Отправлять его некуда, а вызвать все 36 методов подряд — это
    /// заметное на глаз поведение, поэтому оно запрещено явно.
    /// </summary>
    [Fact]
    public async Task Dispatch_ignores_event_without_case()
    {
        var (proxy, relay) = BuildRelay();

        await relay.DispatchAsync(new TelegramusEvent(), TestContext.Current.CancellationToken);

        Assert.Empty(SentMethodNames(proxy));
    }

    /// <summary>
    /// Остановленный токен не должен приводить к отправке: иначе после
    /// остановки хоста реле успело бы вызвать хаб по событию из очереди.
    /// </summary>
    [Fact]
    public async Task Dispatch_does_not_send_when_cancelled()
    {
        var (proxy, relay) = BuildRelay();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.DispatchAsync(new TelegramusEvent { Credits = new EmptyEvent() }, cancelled.Token)
        );

        Assert.Empty(SentMethodNames(proxy));
    }

    /// <summary>
    /// Подписка реле создаётся с увеличенной ёмкостью. Со стандартными 64
    /// всплеск наград переполнял бы очередь реле, и <c>DropWrite</c> отбросил бы
    /// события для всех клиентов хаба, а не только для отставшего: реле читает
    /// медленнее gRPC-стрима, потому что между чтением и отправкой стоит сеть.
    /// </summary>
    [Fact]
    public void Relay_subscribes_with_headroom()
    {
        Assert.True(
            HubEventRelay.SubscriptionCapacity >= 512,
            $"Ёмкость {HubEventRelay.SubscriptionCapacity} меньше допустимого отставания"
        );
    }

    /// <summary>
    /// Событие с нужной веткой. Полезная нагрузка для проверки раскладки не
    /// важна, поэтому кладётся минимальный экземпляр нужного типа.
    /// </summary>
    /// <remarks>
    /// Забытая здесь ветка не делает тест слабее: <c>SampleFor</c> вернёт пустую
    /// оболочку, реле не вызовет ничего, и сравнение с <c>manifest[index]</c>
    /// упадёт. То есть проверка остаётся строгой.
    /// </remarks>
    private static TelegramusEvent SampleFor(TelegramusEvent.EventOneofCase testCase) =>
        testCase switch
        {
            TelegramusEvent.EventOneofCase.Alert => new() { Alert = new AlertEvent() },
            TelegramusEvent.EventOneofCase.Alerts => new() { Alerts = new AlertsEvent() },
            TelegramusEvent.EventOneofCase.WaifuRoll => new() { WaifuRoll = new WaifuRollEvent() },
            TelegramusEvent.EventOneofCase.AddNewWaifu => new()
            {
                AddNewWaifu = new AddNewWaifuEvent(),
            },
            TelegramusEvent.EventOneofCase.ShowCurrentWife => new()
            {
                ShowCurrentWife = new ShowCurrentWifeEvent(),
            },
            TelegramusEvent.EventOneofCase.MergeWaifu => new()
            {
                MergeWaifu = new MergeWaifuEvent(),
            },
            TelegramusEvent.EventOneofCase.UpdateWaifuPrizes => new()
            {
                UpdateWaifuPrizes = new PrizesEvent(),
            },
            TelegramusEvent.EventOneofCase.FumoFriday => new()
            {
                FumoFriday = new FumoFridayEvent(),
            },
            TelegramusEvent.EventOneofCase.NewMessage => new()
            {
                NewMessage = new NewMessageEvent(),
            },
            TelegramusEvent.EventOneofCase.DeleteMessage => new()
            {
                DeleteMessage = new DeleteMessageEvent(),
            },
            TelegramusEvent.EventOneofCase.Highlite => new() { Highlite = new HighliteEvent() },
            TelegramusEvent.EventOneofCase.PostTwitchInfo => new()
            {
                PostTwitchInfo = new PostTwitchInfoEvent(),
            },
            TelegramusEvent.EventOneofCase.MakeScreenParticles => new()
            {
                MakeScreenParticles = new ScreenParticlesEvent(),
            },
            TelegramusEvent.EventOneofCase.MakeScreenEmojisParticles => new()
            {
                MakeScreenEmojisParticles = new ScreenEmojisParticlesEvent(),
            },
            TelegramusEvent.EventOneofCase.RandomMem => new() { RandomMem = new AlertEvent() },
            TelegramusEvent.EventOneofCase.AutoMessage => new()
            {
                AutoMessage = new AutoMessageEvent(),
            },
            TelegramusEvent.EventOneofCase.Adhd => new() { Adhd = new AdhdEvent() },
            TelegramusEvent.EventOneofCase.Explosion => new() { Explosion = new EmptyEvent() },
            TelegramusEvent.EventOneofCase.LeroyAlert => new() { LeroyAlert = new EmptyEvent() },
            TelegramusEvent.EventOneofCase.GaoAlert => new() { GaoAlert = new GaoAlertEvent() },
            TelegramusEvent.EventOneofCase.Credits => new() { Credits = new EmptyEvent() },
            TelegramusEvent.EventOneofCase.MichaelJackson => new()
            {
                MichaelJackson = new EmptyEvent(),
            },
            TelegramusEvent.EventOneofCase.MikuMonday => new()
            {
                MikuMonday = new MikuMondayEvent(),
            },
            TelegramusEvent.EventOneofCase.MikuMikuBeam => new()
            {
                MikuMikuBeam = new MikuMikuBeamEvent(),
            },
            TelegramusEvent.EventOneofCase.PhonkEdit => new() { PhonkEdit = new EmptyEvent() },
            TelegramusEvent.EventOneofCase.TikTokEdit => new()
            {
                TikTokEdit = new TikTokEditEvent(),
            },
            TelegramusEvent.EventOneofCase.AllRefund => new() { AllRefund = new AllRefundEvent() },
            TelegramusEvent.EventOneofCase.AudioQuizStart => new()
            {
                AudioQuizStart = new AudioQuizStartEvent(),
            },
            TelegramusEvent.EventOneofCase.AudioQuizStop => new()
            {
                AudioQuizStop = new EmptyEvent(),
            },
            TelegramusEvent.EventOneofCase.FumoRoll => new() { FumoRoll = new FumoRollEvent() },
            TelegramusEvent.EventOneofCase.UpdateFumoPrizes => new()
            {
                UpdateFumoPrizes = new PrizesEvent(),
            },
            TelegramusEvent.EventOneofCase.FrogRoll => new() { FrogRoll = new FrogRollEvent() },
            TelegramusEvent.EventOneofCase.UpdateFrogPrizes => new()
            {
                UpdateFrogPrizes = new PrizesEvent(),
            },
            TelegramusEvent.EventOneofCase.MikuRoll => new() { MikuRoll = new MikuRollEvent() },
            TelegramusEvent.EventOneofCase.UpdateMikuPrizes => new()
            {
                UpdateMikuPrizes = new PrizesEvent(),
            },
            TelegramusEvent.EventOneofCase.AdhdConfig => new()
            {
                AdhdConfig = new AdhdConfigEvent(),
            },
            _ => new TelegramusEvent(),
        };
}
