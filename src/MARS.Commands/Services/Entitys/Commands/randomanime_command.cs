using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Случайное аниме с Shikimori. Данные и ссылка собираются в
/// <c>MARS.WaifuGacha</c> — там, где живёт клиент Shikimori и лимитер его
/// запросов.
/// </summary>
public class RandomAnimeCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "randomanime";
    public override string Description => "Показать случайное аниме с Shikimori";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var anime = await waifuGachaClient.GetRandomAnimeAsync(cancellationToken);

        return anime is null
            ? CommandResult.Fail(
                "Не удалось получить случайное аниме, попробуйте позже.",
                CommandErrorCode.TargetUnreachable
            )
            : CommandResult.Ok(ShikimoriTitleText.Format(anime));
    }
}
