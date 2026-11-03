using MARS.Alerts.Extensions;
using MARS.Shared.Matoi;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда RANDOM ART: по тегу из пользовательского ввода ищет посты booru через
/// matoi и отдаёт их медиа активации.
/// </summary>
/// <remarks>
/// <para>
/// Ввод зрителя — это <c>тег</c> либо <c>провайдер:тег</c>. Провайдер по
/// умолчанию приходит из настроек: требовать от зрителя префикс значит запретить
/// то, что он писал годами.
/// </para>
/// <para>
/// Пустой ввод или отсутствие пригодных постов — это <see
/// cref="RewardEffectResult.Suppressed"/>: рисовать нечего, и клиент получил бы
/// пустой экран. Рикролл сюда не входит — его делает потребитель до вызова
/// эффекта, один раз на все роллящиеся награды.
/// </para>
/// </remarks>
public sealed class RandomArtEffect(
    IMatoiPostService matoiService,
    IOptions<MatoiOptions> matoiOptions,
    ILogger<RandomArtEffect> logger
) : IRewardActivationEffect
{
    private const int PostCount = 3;

    public string AlertKey => RewardAlertKeys.RandomArt;

    public async Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        var result = RewardEffectResult.Suppressed;
        var userInput = context.UserInput?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userInput))
        {
            logger.LogWarning("RandomArt: empty user input from {UserName}", context.UserName);
        }
        else if (userInput.Contains(' '))
        {
            logger.LogWarning(
                "RandomArt: user input contains spaces from {UserName}",
                context.UserName
            );
        }
        else
        {
            var (provider, tags) = SplitProvider(userInput, matoiOptions.Value.DefaultProvider);
            var posts = await matoiService.GetSafePostsAsync(
                provider,
                tags,
                PostCount,
                cancellationToken
            );

            if (posts is not { Success: true, Result: { Count: > 0 } found })
            {
                logger.LogWarning(
                    "RandomArt: no arts for {Provider}:{Tags}, reason: {Reason}",
                    provider,
                    tags,
                    posts?.Message ?? "неизвестно"
                );
            }
            else
            {
                var media = new List<MediaInfo>(found.Count);

                foreach (var post in found)
                {
                    // Ссылка на файл у matoi приходит уже полной: превью и сэмпл —
                    // это другой размер, а оверлею нужен исходник. Без ссылки
                    // пост нечего показать, и он просто выбрасывается.
                    var fileUrl = post.FileUrl;

                    if (string.IsNullOrWhiteSpace(fileUrl))
                    {
                        logger.LogWarning("RandomArt: no file URL for post {PostId}", post.Id);
                        continue;
                    }

                    var extension = Path.GetExtension(fileUrl);

                    media.Add(
                        new MediaInfo
                        {
                            FileInfo = new MediaFileInfo
                            {
                                Extension = extension,
                                FileName = Path.GetFileName(fileUrl),
                                FilePath = fileUrl,
                                Type = await extension.GetFileMediaTypeAsync(),
                                IsLocalFile = false,
                            },
                            MetaInfo = new MediaMetaInfo { DisplayName = context.UserName },
                            PositionInfo = new MediaPositionInfo(),
                            StylesInfo = new MediaStylesInfo(),
                            TextInfo = new MediaTextInfo(),
                        }
                    );
                }

                if (media.Count == 0)
                {
                    logger.LogWarning(
                        "RandomArt: every post for {Provider}:{Tags} was unusable, nothing to send",
                        provider,
                        tags
                    );
                }
                else
                {
                    result = new RewardEffectResult { Media = media };
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Разбирает ввод на провайдера и теги. Провайдер без двоеточия — тот, что
    /// задан настройкой.
    /// </summary>
    internal static (string Provider, string Tags) SplitProvider(
        string userInput,
        string defaultProvider
    )
    {
        var separator = userInput.IndexOf(':');

        if (separator > 0 && separator < userInput.Length - 1)
        {
            return (userInput[..separator], userInput[(separator + 1)..]);
        }

        return (defaultProvider, userInput);
    }
}
