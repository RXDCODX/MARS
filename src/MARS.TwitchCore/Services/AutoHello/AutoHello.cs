using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.AutoHello;

public class AutoHello(
    ILogger<AutoHello> logger,
    ITwitchClient client,
    IAutoHelloService autoHelloService,
    IHostApplicationLifetime applicationLifetime,
    ITwitchEventValidationService validator
) : BackgroundService
{
    public async Task AutoHelloTwitchEvent(object? sender, OnMessageReceivedArgs args)
    {
        var result = await validator
            .ForMessageReceived(args)
            .RequireChannel()
            .SkipBlacklisted()
            .ValidateWithResponseAsync(args.ChatMessage.Username);

        if (result.IsInvalid)
        {
            return;
        }

        await Task.Run(async () =>
        {
            var message = await autoHelloService.GetAutoHelloMessageAsync(
                args.ChatMessage.UserId,
                args.ChatMessage.Username
            );

            if (!string.IsNullOrWhiteSpace(message))
            {
                await client.SendMessageToMainTwitchAsync(message, logger);
            }
        });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += AutoHelloTwitchEvent;
        });

        return Task.CompletedTask;
    }
}
