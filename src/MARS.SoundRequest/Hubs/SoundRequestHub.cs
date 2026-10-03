using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.SoundRequest.Hubs;

/// <summary>
/// Хаб звуковых запросов.
/// </summary>
/// <remarks>
/// Устроен так же, как хабы оверлея и трека: своего состояния нет, всё
/// приходит из широковещателя через реле.
/// </remarks>
public class SoundRequestHub : Hub<ISoundRequestHub>;
