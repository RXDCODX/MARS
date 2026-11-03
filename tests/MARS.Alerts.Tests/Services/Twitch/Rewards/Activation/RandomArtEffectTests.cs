using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Matoi;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда RANDOM ART: поиск постов через matoi.
/// </summary>
/// <remarks>
/// Пустой ввод и «ничего не нашлось» — это подавление, а не пустое событие:
/// клиент иначе показал бы пустой экран. Провайдер по умолчанию приходит из
/// настроек, потому что зритель писал только тег.
/// </remarks>
public class RandomArtEffectTests
{
    private const string DefaultProvider = "danbooru";

    private readonly Mock<IMatoiPostService> _matoi = new();
    private string? _provider;
    private string? _tags;

    public RandomArtEffectTests()
    {
        _matoi
            .Setup(service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, int, CancellationToken>(
                (provider, tags, _, _) =>
                {
                    _provider = provider;
                    _tags = tags;
                }
            )
            .ReturnsAsync(
                OperationResult<IReadOnlyList<MatoiPost>>.Ok([Post("https://cdn.example/a.png")])
            );
    }

    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(RewardAlertKeys.RandomArt, CreateEffect().AlertKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyInputIsSuppressed(string? userInput)
    {
        var result = await CreateEffect()
            .ApplyAsync(Context(userInput), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
        _matoi.Verify(
            service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task InputWithSpacesIsSuppressed()
    {
        var result = await CreateEffect()
            .ApplyAsync(Context("two words"), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
        _matoi.Verify(
            service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task FoundPostsBecomeMedia()
    {
        Posts(
            Post("https://cdn.example/a.png"),
            Post("https://cdn.example/b.mp4"),
            Post("https://cdn.example/c.gif")
        );

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.False(result.Suppress);
        Assert.Equal(
            [MediaType.Image, MediaType.Video, MediaType.Gif],
            result.Media!.Select(media => media.FileInfo.Type)
        );
        Assert.All(
            result.Media!,
            media =>
            {
                Assert.False(media.FileInfo.IsLocalFile);
                Assert.Equal("Pyro", media.MetaInfo.DisplayName);
                Assert.StartsWith("https://cdn.example/", media.FileInfo.FilePath);
            }
        );
    }

    [Fact]
    public async Task ProviderFromInputOverridesDefault()
    {
        await CreateEffect()
            .ApplyAsync(Context("safebooru:cute_cat"), TestContext.Current.CancellationToken);

        Assert.Equal("safebooru", _provider);
        Assert.Equal("cute_cat", _tags);
    }

    [Fact]
    public async Task DefaultProviderIsUsedWithoutPrefix()
    {
        await CreateEffect().ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.Equal(DefaultProvider, _provider);
        Assert.Equal("cute_cat", _tags);
    }

    [Theory]
    [InlineData(":tag")]
    [InlineData("provider:")]
    public async Task IncompleteProviderPrefixFallsBackToDefault(string userInput)
    {
        await CreateEffect().ApplyAsync(Context(userInput), TestContext.Current.CancellationToken);

        Assert.Equal(DefaultProvider, _provider);
        Assert.Equal(userInput, _tags);
    }

    /// <summary>
    /// Пост без ссылки на файл показать нечем, он выбрасывается, а остальные
    /// остаются: один битый пост не должен отменять всю награду.
    /// </summary>
    [Fact]
    public async Task PostsWithoutFileUrlAreDropped()
    {
        Posts(Post(null), Post("https://cdn.example/b.png"));

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.Equal("https://cdn.example/b.png", Assert.Single(result.Media!).FileInfo.FilePath);
    }

    [Fact]
    public async Task AllPostsWithoutFileUrlIsSuppressed()
    {
        Posts(Post(null));

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
    }

    [Fact]
    public async Task FailedSearchIsSuppressed()
    {
        _matoi
            .Setup(service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(OperationResult<IReadOnlyList<MatoiPost>>.Fail("matoi недоступен"));

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
    }

    [Fact]
    public async Task EmptySearchResultIsSuppressed()
    {
        Posts();

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
    }

    [Fact]
    public async Task SearchResultWithoutListIsSuppressed()
    {
        _matoi
            .Setup(service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new OperationResult<IReadOnlyList<MatoiPost>> { Success = true });

        var result = await CreateEffect()
            .ApplyAsync(Context("cute_cat"), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
    }

    private RandomArtEffect CreateEffect() =>
        new(
            _matoi.Object,
            Options.Create(new MatoiOptions { DefaultProvider = DefaultProvider }),
            NullLogger<RandomArtEffect>.Instance
        );

    private void Posts(params MatoiPost[] posts) =>
        _matoi
            .Setup(service =>
                service.GetSafePostsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(OperationResult<IReadOnlyList<MatoiPost>>.Ok(posts));

    private static MatoiPost Post(string? fileUrl) => new() { Id = 1, FileUrl = fileUrl };

    private static RewardActivationContext Context(string? userInput) =>
        new()
        {
            Reward = new RewardRedeemedEvent
            {
                AlertKey = RewardAlertKeys.RandomArt,
                UserId = "42",
                UserName = "Pyro",
                UserInput = userInput,
                Cost = 500,
            },
            Media = [],
        };
}
