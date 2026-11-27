namespace MARS.Platform.Infrastructure.Grpc.Twitch;

public sealed record TwitchConnectionState(
    bool IsEventSubConnected,
    bool IsTwitchChatConnected,
    string JoinedChannels = "",
    string LastConnectedAt = "",
    string LastDisconnectedAt = "",
    string LastDisconnectReason = "",
    int ReconnectAttempts = 0,
    bool IsReconnecting = false
);