using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.MediaStorage;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class GetAllKeyWordsForAlertsCommand : BaseCommand
{
    public override string CommandName => "getAllKeyWordsForAlerts";
    public override string Description => "Получить все ключевые слова для активации алертов";
    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public GetAllKeyWordsForAlertsCommand(IMarsGrpcClient grpcClient)
        : base()
    {
        _grpcClient = grpcClient;
    }

    private readonly IMarsGrpcClient _grpcClient;

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var alerts = await _grpcClient.MediaStorage.GetAllAlertsAsync(cancellationToken) ?? [];
            var keywords = alerts
                .Select(alert => alert.TextInfo?.TriggerWord)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return CommandResult.Ok(
                keywords.Length == 0 ? "Ключевые слова не найдены." : string.Join("#", keywords)
            );
        }
        catch
        {
            return CommandResult.Fail(
                "Не удалось получить ключевые слова алертов.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
