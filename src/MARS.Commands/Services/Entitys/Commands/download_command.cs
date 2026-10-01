using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class DownloadCommand : BaseCommand
{
    public override string CommandName => "download";
    public override string Description => "Скачать трек/видео (YouTube, SoundCloud)";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Discord];

    public override string[] Aliases => ["ytdownload", "dl"];

    public override CommandVisibility Visibility => CommandVisibility.All;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "url",
                Description = "URL видео с YouTube или SoundCloud",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    // В монолите здесь были ещё message, discord_channel_id и discord_message_id с
    // типами Message и ulong. Это не аргументы: их подставлял адаптер платформы из
    // события, и объявление в Parameters заставляло ParseParameters искать их среди
    // пользовательского ввода. Типов Message и ulong нет и не должно быть —
    // ConvertValue всё равно не умеет их собирать, а молча отдавал строку.
    // Контекст разговора приходит отдельно (conversation_id в commands.proto).

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            CommandResult.Fail(
                "Команда ещё не реализована: нужные сервисы не подключены.",
                CommandErrorCode.NotImplemented
            )
        );
    }
}
