using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.MediaStorage;
using Microsoft.Extensions.Logging;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class DownloadCommand(IMarsGrpcClient grpcClient, ILogger<DownloadCommand> logger)
    : BaseCommand
{
    public override string CommandName => "download";

    public override string Description => "Скачать трек/видео (YouTube, SoundCloud)";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Discord];

    public override string[] Aliases => ["ytdownload", "dl"];

    public override CommandParameterInfo[] Parameters =>
        [
            new CommandParameterInfo
            {
                Name = "url",
                Type = CommandParameterType.String,
                Required = true,
                Description = "URL видео с YouTube или SoundCloud",
            },
        ];

    public override bool IsDurable => true;

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var url = parameters.TryGetValue("url", out var rawUrl) ? rawUrl?.ToString()?.Trim() : null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return CommandResult.Fail(
                "❌ Необходимо указать URL видео/трека",
                CommandErrorCode.BadArguments
            );
        }

        try
        {
            var response = await grpcClient.MediaStorage.CreateDownloadJobAsync(
                new CreateDownloadJobRequest
                {
                    Url = url,
                    ActorId = parameters.TryGetValue("userId", out var userId)
                        ? userId?.ToString() ?? string.Empty
                        : string.Empty,
                    Platform = platform.ToString(),
                    ExecutionId = Guid.NewGuid().ToString("N"),
                    CorrelationId = Guid.NewGuid().ToString("N"),
                },
                cancellationToken
            );

            var title = string.IsNullOrWhiteSpace(response.Title) ? url : response.Title;
            var shortJobId = response.JobId.Length >= 8 ? response.JobId[..8] : response.JobId;

            var ack =
                response.ArtifactKind == 2
                    ? $"✅ Загрузка аудио начата: {title}\n⏳ Скачивание может занять время...\n🆔 Job: {shortJobId}\nСтатус и отмена: Commands UI."
                    : $"✅ Видео начинает обрабатываться: {title}\n⏳ Скачивание может занять время...\n🆔 Job: {shortJobId}\nСтатус и отмена: Commands UI.";

            return CommandResult.Accepted(ack, response.JobId);
        }
        catch (RpcException ex)
            when (ex.Status.StatusCode is StatusCode.InvalidArgument or StatusCode.NotFound)
        {
            logger.LogWarning(ex, "Download job rejected for {Url}", url);
            return CommandResult.Fail(
                $"❌ Не удалось создать задачу загрузки: {ex.Status.Detail}",
                CommandErrorCode.BadArguments
            );
        }
        catch (RpcException ex)
            when (ex.Status.StatusCode is StatusCode.Unavailable or StatusCode.PermissionDenied)
        {
            logger.LogWarning(ex, "MediaStorage unavailable for {Url}", url);
            return CommandResult.Fail(
                $"❌ Сервис медиа недоступен: {ex.Status.Detail}",
                CommandErrorCode.TargetUnreachable
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download job creation failed for {Url}", url);
            return CommandResult.Fail(
                $"❌ Ошибка при обработке: {ex.Message}",
                CommandErrorCode.Failed
            );
        }
    }
}
