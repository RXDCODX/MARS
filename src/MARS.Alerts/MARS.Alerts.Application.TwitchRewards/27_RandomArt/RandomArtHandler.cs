using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Extensions;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Http.Matoi;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик награды RANDOM ART: по тегу из пользовательского ввода ищет посты
/// booru через matoi и отправляет их во фронтенд.
/// </summary>
/// <remarks>
/// Ввод зрителя — это <c>тег</c> либо <c>провайдер:тег</c>. Провайдер по
/// умолчанию приходит из настроек: требовать от зрителя префикс значит запретить
/// то, что он писал годами, а проверку рейтинга и разбор провайдеров берёт на себя
/// клиент — второй список провайдеров в обработчике разошёлся бы с ним тихо.
/// </remarks>
public class RandomArtHandler(
    ILogger<RandomArtHandler> logger,
    IMatoiPostService matoiService,
    RickRollerService rickRollerService,
    IOptions<MatoiOptions> matoiOptions,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    private const int PostCount = 3;

    public override int RewardCost { get; } = 27;
    public override string AlertDisplayName { get; set; } = "🎨 Random Art";
    public override string AlertDescription { get; set; } =
        "Введи только 1 тег, Example: black_hair";
    public override Color Color { get; set; } = Color.FromArgb(145, 71, 255);
    public override int Cost { get; init; } = 27;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override IRewardActivationPolicy Activation => new ServiceActivePolicy(() => true);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled)
        );
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var userInput = rewardEvent.UserInput?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userInput))
        {
            logger.LogWarning("RandomArt: empty user input from {UserName}", rewardEvent.UserName);
            return;
        }

        if (userInput.Contains(' '))
        {
            logger.LogWarning(
                "RandomArt: user input contains spaces from {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        var (provider, tags) = SplitProvider(userInput, matoiOptions.Value.DefaultProvider);

        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => ProcessRandomArtAsync(provider, tags, rewardEvent.UserName, ct)
            );
        }
        else
        {
            await ProcessRandomArtAsync(provider, tags, rewardEvent.UserName, ct);
        }
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

    private async Task ProcessRandomArtAsync(
        string provider,
        string tags,
        string userName,
        CancellationToken ct
    )
    {
        var result = await matoiService.GetSafePostsAsync(provider, tags, PostCount, ct);

        if (result is not { Success: true, Result: { Count: > 0 } posts })
        {
            logger.LogWarning(
                "RandomArt: no arts for {Provider}:{Tags}, reason: {Reason}",
                provider,
                tags,
                result?.ErrorMessage ?? "неизвестно"
            );
            return;
        }

        var mediaDtos = new List<MediaInfo>(posts.Count);

        foreach (var post in posts)
        {
            // Ссылка на файл у matoi приходит уже полной: превью и сэмпл — это
            // другой размер, а оверлею нужен исходник. Без ссылки пост нечего
            // показать, и он просто выбрасывается.
            var fileUrl = post.FileUrl;

            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                logger.LogWarning("RandomArt: no file URL for post {PostId}", post.Id);
                continue;
            }

            var extension = Path.GetExtension(fileUrl);
            var fileName = Path.GetFileName(fileUrl);
            var mediaType = await extension.GetFileMediaTypeAsync();

            mediaDtos.Add(
                new MediaInfo
                {
                    FileInfo = new MediaFileInfo
                    {
                        Extension = extension,
                        FileName = fileName,
                        FilePath = fileUrl,
                        Type = mediaType,
                        IsLocalFile = false,
                    },
                    MetaInfo = new MediaMetaInfo { DisplayName = userName },
                    PositionInfo = new MediaPositionInfo(),
                    StylesInfo = new MediaStylesInfo(),
                    TextInfo = new MediaTextInfo(),
                }
            );
        }

        if (mediaDtos.Count == 0)
        {
            logger.LogWarning(
                "RandomArt: every post for {Provider}:{Tags} was unusable, nothing to send",
                provider,
                tags
            );
            return;
        }

        var alertsEvent = new AlertsEvent();
        alertsEvent.Media.AddRange(mediaDtos);

        await hubContext.Clients.All.Alerts(alertsEvent);
    }
}
