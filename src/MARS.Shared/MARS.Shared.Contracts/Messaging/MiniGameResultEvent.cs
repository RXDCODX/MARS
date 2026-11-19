namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Результат мини-игры (рулетка, викторина, аудиовикторина) для записи
/// в таблицу лидерборда. Публикуют сервисы, где проходит игра;
/// обрабатывает MARS.Twitch.Api — владелец таблицы.
/// </summary>
public sealed class MiniGameResultEvent : IMarsEventMessage
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Game { get; init; } = string.Empty;
    public bool WithWaifu { get; init; }
    public int Score { get; init; } = 1;
}
