namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>Состояние воспроизведения в терминах клиента.</summary>
/// <remarks>
/// Повторяет <c>PlayerStateStateEnum</c> из контракта клиента
/// (<c>data-contracts.ts</c>), а не доменный <c>PlaybackState</c> и не proto.
/// <para>
/// Набор значений обязан совпадать с клиентским целиком. Раньше здесь было
/// три значения из пяти, и разбор падал с <c>JsonException</c> на
/// <c>"SwitchingTrack"</c> и <c>"WaitingForTrack"</c> — а <c>StateManager</c>
/// ставит <c>WaitingForTrack</c> при каждом переключении трека. После первого
/// же трека mute, громкость, play и переключение видео падали все разом, пока
/// страница не перезагрузится.
/// </para>
/// </remarks>
public enum PlayerStateStateEnum
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
    SwitchingTrack = 3,
    WaitingForTrack = 4,
}
