using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Scoreboard;
using ProtoScoreboardService = MARS.Shared.Grpc.Scoreboard.ScoreboardService;
using ScoreboardService = MARS.Scoreboard.Services.ScoreboardService;

namespace MARS.Scoreboard.Grpc;

public sealed class ScoreboardGrpcService(
    ScoreboardService scoreboardService,
    GrpcEventBroadcaster<ScoreboardEvent> broadcaster,
    ILogger<ScoreboardGrpcService> logger
) : ProtoScoreboardService.ScoreboardServiceBase
{
    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<ScoreboardEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe();
        var state = await scoreboardService.GetCurrentStateAsync();

        broadcaster.TryEnqueue(
            subscription,
            new ScoreboardEvent { StateUpdated = ScoreboardGrpcMapper.ToProto(state) }
        );

        logger.LogInformation("Client subscribed to scoreboard: {SubscriberId}", subscription.Id);

        await broadcaster.PumpAsync(subscription, responseStream, context.CancellationToken);
    }

    public override async Task<GetCurrentStateResponse> GetCurrentState(
        GetCurrentStateRequest request,
        ServerCallContext context
    )
    {
        var state = await scoreboardService.GetCurrentStateAsync();

        return new GetCurrentStateResponse { State = ScoreboardGrpcMapper.ToProto(state) };
    }

    public override async Task<UpdateStateResponse> UpdateState(
        UpdateStateRequest request,
        ServerCallContext context
    )
    {
        await scoreboardService.UpdateStateAsync(ScoreboardGrpcMapper.ToDto(request.State));

        await broadcaster.BroadcastAsync(new ScoreboardEvent { StateUpdated = request.State });

        return new UpdateStateResponse();
    }

    public override async Task<UpdatePlayerScoreResponse> UpdatePlayerScore(
        UpdatePlayerScoreRequest request,
        ServerCallContext context
    )
    {
        var success = await scoreboardService.UpdatePlayerScoreAsync(
            request.PlayerPosition,
            request.NewScore
        );

        if (success)
        {
            await broadcaster.BroadcastAsync(
                new ScoreboardEvent
                {
                    PlayerScoreUpdated = new PlayerScoreUpdated
                    {
                        PlayerPosition = request.PlayerPosition,
                        NewScore = request.NewScore,
                    },
                }
            );
        }

        return new UpdatePlayerScoreResponse { Success = success };
    }

    public override async Task<SetPlayerFinalResponse> SetPlayerFinal(
        SetPlayerFinalRequest request,
        ServerCallContext context
    )
    {
        var success = await scoreboardService.SetPlayerFinalAsync(
            request.PlayerPosition,
            request.Final
        );

        if (success)
        {
            await broadcaster.BroadcastAsync(
                new ScoreboardEvent
                {
                    PlayerFinalUpdated = new PlayerFinalUpdated
                    {
                        PlayerPosition = request.PlayerPosition,
                        Final = request.Final,
                    },
                }
            );
        }

        return new SetPlayerFinalResponse { Success = success };
    }

    public override async Task<SetVisibilityResponse> SetVisibility(
        SetVisibilityRequest request,
        ServerCallContext context
    )
    {
        var success = await scoreboardService.SetVisibilityAsync(request.IsVisible);

        if (success)
        {
            await broadcaster.BroadcastAsync(
                new ScoreboardEvent
                {
                    VisibilityChanged = new VisibilityChanged { IsVisible = request.IsVisible },
                }
            );
        }

        return new SetVisibilityResponse { Success = success };
    }
}
