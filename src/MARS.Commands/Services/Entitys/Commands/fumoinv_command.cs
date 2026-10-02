using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Инвентарь собранных фумо. Коллекция лежит в <c>MARS.WaifuGacha</c>, а имя
/// пользователя в логин превращается там же — через справочник
/// <c>MARS.TwitchCore</c>.
/// </summary>
public class FumoInventoryCommand(
    IWaifuGachaClient waifuGachaClient,
    ITwitchUserClient twitchUserClient
) : BaseCommand
{
    public override string CommandName => "fumoinv";
    public override string Description => "Показывает инвентарь фумо";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "displayName",
                Description = "Имя пользователя (опционально)",
                Type = CommandParameterType.String,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var twitchId = await CollectionTargetResolver.ResolveAsync(
            parameters,
            twitchUserClient,
            cancellationToken
        );

        if (twitchId.TwitchId is null && twitchId.Login is not null)
        {
            return CommandResult.Fail(
                $"Пользователь {twitchId.Login} не найден",
                CommandErrorCode.BadArguments
            );
        }

        if (twitchId.TwitchId is null)
        {
            return CommandResult.Fail(
                "Не удалось определить пользователя.",
                CommandErrorCode.BadArguments
            );
        }

        var inventory = await waifuGachaClient.GetFumoInventoryAsync(
            twitchId.TwitchId,
            cancellationToken
        );

        if (inventory is null)
        {
            return CommandResult.Fail(
                "Не удалось получить инвентарь: сервис недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(
            CollectionInventoryText.Format(
                twitchId.DisplayName ?? twitchId.TwitchId,
                CollectionInventoryView.From(inventory),
                "фумо"
            )
        );
    }
}
