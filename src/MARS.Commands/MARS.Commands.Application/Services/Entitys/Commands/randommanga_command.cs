using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

/// <summary>
/// Случайная манга с Shikimori. Данные и ссылка собираются в
/// <c>MARS.WaifuGacha</c> — там, где живёт клиент Shikimori и лимитер его
/// запросов.
/// </summary>
public class RandomMangaCommand(IMarsGrpcClient grpcClient) : BaseCommand
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
        MARS.Shared.Contracts.Clients.ShikimoriTitleRef? manga;
        try
        {
            manga = await grpcClient.WaifuGacha.GetRandomMangaAsync(cancellationToken);
        }
        catch (RpcException)
        {
            manga = null;
        }

        return manga is null
            ? CommandResult.Fail(
                "Не удалось получить случайную мангу, попробуйте позже.",
                CommandErrorCode.TargetUnreachable
            )
            : CommandResult.Ok(ShikimoriTitleText.Format(manga));
    }
}
