using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Matoi;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Обработчик награды RANDOM ART поверх общего клиента matoi.
/// </summary>
/// <remarks>
/// Проверяется ввод пользователя, а не HTTP: адрес, авторизация, рейтинг и
/// обход страниц живут в <see cref="IMatoiPostService"/> и закрыты в
/// <c>MARS.Shared.Tests</c>. Здесь важно, что обработчик разбирает ввод,
/// не теряет результат и отправляет в оверлей ровно то, что вернул сервис.
/// </remarks>
public class RandomArtHandlerTests
{
    private static readonly CancellationToken Token = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ПровайдерИзВводаПередаётсяВСервис()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/a.png"));

        await handler.HandleAsync(Event("rule34:maid"), Token);

        Assert.Equal("rule34", source.Calls.Single().Provider);
        Assert.Equal("maid", source.Calls.Single().Tags);
    }

    [Fact]
    public async Task БезДвоеточияБерётсяПровайдерПоУмолчанию()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/a.png"));

        await handler.HandleAsync(Event("hatsune_miku"), Token);

        // Ввод зрителя — это тег, а провайдер выбирает настройка: требовать от
        // него «danbooru:тег» значит запретить то, что он писал годами.
        Assert.Equal("danbooru", source.Calls.Single().Provider);
        Assert.Equal("hatsune_miku", source.Calls.Single().Tags);
    }

    [Fact]
    public async Task ПровайдерНеизвестныйУходитВСервисИНеОтправляетОверлей()
    {
        var source = new AlertSource();
        var handler = Create(source, Fail("Неизвестный провайдер: gelbooru"));

        await handler.HandleAsync(Event("gelbooru:maid"), Token);

        // Решение о неизвестных провайдерах принимает словарь в клиенте: обработчик
        // не заводит второго списка, который разошёлся бы с первым.
        Assert.Single(source.Calls);
        Assert.Equal("gelbooru", source.Calls[0].Provider);
        Assert.Empty(source.Alerts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ПустойВводНеХодитВСервис(string input)
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/a.png"));

        await handler.HandleAsync(Event(input), Token);

        Assert.Empty(source.Calls);
        Assert.Empty(source.Alerts);
    }

    [Fact]
    public async Task ВводСПробеломОтклоняется()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/a.png"));

        await handler.HandleAsync(Event("hatsune miku"), Token);

        // Пробел в разделителе тегов привёл бы к молчаливой выдаче мусора:
        // matoi склеивает теги в одну фразу и не находит ничего.
        Assert.Empty(source.Calls);
        Assert.Empty(source.Alerts);
    }

    [Fact]
    public async Task ОтказСервисаНеОтправляетОверлей()
    {
        var source = new AlertSource();
        var handler = Create(source, Fail("matoi недоступен"));

        await handler.HandleAsync(Event("tag"), Token);

        Assert.Empty(source.Alerts);
    }

    [Fact]
    public async Task ПустойСписокПостовНеОтправляетОверлей()
    {
        var source = new AlertSource();
        var handler = Create(source, Ok([]));

        await handler.HandleAsync(Event("tag"), Token);

        Assert.Empty(source.Alerts);
    }

    [Fact]
    public async Task ВОверлейУходитИсходныйUrlФайла()
    {
        var source = new AlertSource();
        var handler = Create(
            source,
            Posts("danbooru", "g", "https://cdn.test/pic.png", "https://cdn.test/pic2.jpg")
        );

        await handler.HandleAsync(Event("tag"), Token);

        Assert.Equal(2, source.Alerts.Count);
        Assert.Equal(
            new[] { "https://cdn.test/pic.png", "https://cdn.test/pic2.jpg" },
            source.Alerts.Select(media => media.MediaInfo.FileInfo.FilePath)
        );
        Assert.All(source.Alerts, media => Assert.False(media.MediaInfo.FileInfo.IsLocalFile));
    }

    [Fact]
    public async Task ПостБезUrlПропускаетсяАОстальныеУходят()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "", "https://cdn.test/pic.png"));

        await handler.HandleAsync(Event("tag"), Token);

        var sent = Assert.Single(source.Alerts);

        Assert.Equal("https://cdn.test/pic.png", sent.MediaInfo.FileInfo.FilePath);
    }

    [Fact]
    public async Task ВсеПостыБезUrlНеОтправляютПустойОверлей()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", ""));

        await handler.HandleAsync(Event("tag"), Token);

        // Пустой вызов ушёл бы на фронтенд и выключил бы текущий медиапоток.
        Assert.Empty(source.Alerts);
    }

    [Fact]
    public async Task РасширениеИТипБерутсяИзUrl()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/pic.png"));

        await handler.HandleAsync(Event("tag"), Token);

        var info = Assert.Single(source.Alerts).MediaInfo.FileInfo;

        Assert.Equal(".png", info.Extension);
        Assert.Equal("pic.png", info.FileName);
        Assert.Equal(MediaType.Image, info.Type);
    }

    [Fact]
    public async Task ЗапросИдётБезДублированияТегаРейтинга()
    {
        var source = new AlertSource();
        var handler = Create(source, Posts("danbooru", "g", "https://cdn.test/pic.png"));

        // Зритель дописал рейтинг сам. Обработчик обязан отдать это как есть:
        // фильтром безопасности занимается клиент, и подмена тега здесь была бы
        // второй реализацией правила, которая с ним разойдётся.
        await handler.HandleAsync(Event("danbooru:rating:g"), Token);

        Assert.Equal("danbooru", source.Calls.Single().Provider);
        Assert.Equal("rating:g", source.Calls.Single().Tags);
    }

    private static OperationResult<IReadOnlyList<MatoiPost>> Posts(
        string provider,
        string rating,
        params string[] urls
    )
    {
        var posts = urls.Select(
                (url, index) =>
                    new MatoiPost
                    {
                        Id = index + 1,
                        Rating = rating,
                        FileUrl = url,
                        PreviewUrl = url,
                        Tags = ["maid"],
                    }
            )
            .ToList();

        return OperationResult<IReadOnlyList<MatoiPost>>.Ok(posts);
    }

    private static OperationResult<IReadOnlyList<MatoiPost>> Fail(string error) =>
        OperationResult<IReadOnlyList<MatoiPost>>.Fail(error);

    private static OperationResult<IReadOnlyList<MatoiPost>> Ok(IReadOnlyList<MatoiPost> posts) =>
        OperationResult<IReadOnlyList<MatoiPost>>.Ok(posts);

    private static RandomArtHandler Create(
        AlertSource source,
        OperationResult<IReadOnlyList<MatoiPost>> result
    )
    {
        var notifier = new Mock<ITelegramusNotifier>();
        notifier
            .Setup(client => client.Alerts(It.IsAny<MediaDto[]>()))
            .Returns(Task.CompletedTask)
            .Callback<MediaDto[]>(media => source.Alerts.AddRange(media));

        var service = new Mock<IMatoiPostService>();
        service
            .Setup(client =>
                client.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (string provider, string tags, int _, CancellationToken _) =>
                {
                    source.Calls.Add(new Call(provider, tags));

                    return Task.FromResult(result);
                }
            );

        return new RandomArtHandler(
            notifier.Object,
            NullLogger<RandomArtHandler>.Instance,
            service.Object,
            new RickRollerService(
                notifier.Object,
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()
            ),
            Options.Create(new MatoiOptions { DefaultProvider = "danbooru" })
        );
    }

    private static RewardRedeemedEvent Event(string input) =>
        new()
        {
            RewardId = "reward-1",
            RewardTitle = "RANDOM ART",
            Cost = 100,
            UserId = "42",
            UserName = "pyro",
            UserInput = input,
            RedeemedAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            MessageId = "job-1",
        };

    private sealed record Call(string Provider, string Tags);

    private sealed class AlertSource
    {
        public List<Call> Calls { get; } = [];

        public List<MediaDto> Alerts { get; } = [];
    }
}
