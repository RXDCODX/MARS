using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Случайная манга с Shikimori. Данные и ссылка собираются в
/// <c>MARS.WaifuGacha</c> — там, где живёт клиент Shikimori и лимитер его
/// запросов.
/// </summary>
public class RandomMangaCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "randommanga";
    public override string Description => "Показать случайную мангу с Shikimori";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var manga = await waifuGachaClient.GetRandomMangaAsync(cancellationToken);

        return manga is null
            ? CommandResult.Fail(
                "Не удалось получить случайную мангу, попробуйте позже.",
                CommandErrorCode.TargetUnreachable
            )
            : CommandResult.Ok(ShikimoriTitleText.Format(manga));
    }
}
