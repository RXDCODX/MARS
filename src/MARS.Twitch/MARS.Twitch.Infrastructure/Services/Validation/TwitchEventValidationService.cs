using System.Collections.Concurrent;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.Twitch.Infrastructure.Services.Validation;

public sealed class TwitchEventValidationService(
    ITwitchClient client,
    ILogger<TwitchEventValidationService> logger,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ITwitchUserEnsureService userEnsureService
) : ITwitchEventValidationService
{
    internal readonly ConcurrentDictionary<string, DateTime> SentEventErrors = new();

    public IMessageValidationBuilder ForMessageReceived(OnMessageReceivedArgs args)
    {
        return new MessageValidationBuilder(args, client, logger, SentEventErrors);
    }

    public IRedemptionValidationBuilder ForRedemption(ChannelPointsCustomRewardRedemptionArgs args)
    {
        return new RedemptionValidationBuilder(
            args,
            client,
            logger,
            dbContextFactory,
            userEnsureService
        );
    }
}
