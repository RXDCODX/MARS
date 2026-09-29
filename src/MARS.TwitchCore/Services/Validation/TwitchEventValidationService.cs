using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Services.Validation;

public sealed class TwitchEventValidationService(
    ITwitchClient client,
    ILogger<TwitchEventValidationService> logger
) : ITwitchEventValidationService
{
    internal readonly ConcurrentDictionary<string, DateTime> SentEventErrors = new();

    public IMessageValidationBuilder ForMessageReceived(OnMessageReceivedArgs args)
    {
        return new MessageValidationBuilder(args, client, logger, SentEventErrors);
    }

    public IRedemptionValidationBuilder ForRedemption(ChannelPointsCustomRewardRedemptionArgs args)
    {
        return new RedemptionValidationBuilder(args, client, logger);
    }
}
