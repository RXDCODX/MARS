using MARS.Alerts.Extensions;
using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик награды RANDOM ART: по тегу из пользовательского ввода ищет посты
/// на Danbooru и отправляет их во фронтенд.
/// </summary>
public class RandomArtHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<RandomArtHandler> logger,
    DanbooruRandomPostService danbooruService,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardRandomArt;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
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

        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => ProcessRandomArtAsync(userInput, rewardEvent.UserName)
            );
        }
        else
        {
            await ProcessRandomArtAsync(userInput, rewardEvent.UserName);
        }
    }

    private async Task ProcessRandomArtAsync(string tag, string userName)
    {
        var searchQuery = $"{tag} rating:general";
        var searchResult = await danbooruService.GetRandomPostAsync(searchQuery);

        if (searchResult is not { Length: > 0 })
        {
            logger.LogWarning("RandomArt: no arts found for tag {Tag}", tag);
            return;
        }

        var mediaDtos = new List<MediaDto>(searchResult.Length);

        foreach (var post in searchResult.DistinctBy(entry => entry.Id))
        {
            var fileUrl = post.LargeFileUrl ?? post.FileUrl ?? post.PreviewFileUrl;

            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                logger.LogWarning("RandomArt: no file URL for post {PostId}", post.Id);
                continue;
            }

            var extension = Path.GetExtension(fileUrl);
            var fileName = Path.GetFileName(fileUrl);
            var mediaType = await extension.GetFileMediaTypeAsync();

            mediaDtos.Add(
                new MediaDto
                {
                    MediaInfo = new MediaInfo
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
                    },
                }
            );
        }

        if (mediaDtos.Count == 0)
        {
            logger.LogWarning(
                "RandomArt: every post for tag {Tag} was unusable, nothing to send",
                tag
            );
            return;
        }

        await hubContext.Clients.All.Alerts([.. mediaDtos]);
    }
}
