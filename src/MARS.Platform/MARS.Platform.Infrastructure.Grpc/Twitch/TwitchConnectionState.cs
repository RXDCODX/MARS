namespace MARS.Platform.Infrastructure.Grpc.Twitch;

public sealed record TwitchConnectionState(bool IsEventSubConnected, bool IsTwitchChatConnected);
