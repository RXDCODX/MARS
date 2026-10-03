namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>Режим видео в терминах клиента.</summary>
/// <remarks>
/// Повторяет <c>PlayerStateVideoStateEnum</c> из контракта клиента. Набор из трёх
/// значений обязателен: клиент переключает видео циклом
/// <c>Video → NoVideo → AudioOnly</c>, то есть уже второе нажатие присылает
/// <c>NoVideo</c>. При двух значениях разбор ронял вызов на втором нажатии.
/// </remarks>
public enum PlayerStateVideoStateEnum
{
    Video = 0,
    NoVideo = 1,
    AudioOnly = 2,
}
