using System.Collections.Generic;

namespace MARS.Shared.Models.Audio;

/// <summary>
/// Запрос на mute микшера приложений. Список имён — исключения: процессы, которые
/// НЕ должны быть заглушены. Пустой список означает значения по умолчанию.
/// </summary>
public sealed class MuteMixerRequest
{
    public IReadOnlyList<string>? ExcludedProcessNames { get; set; }
}
